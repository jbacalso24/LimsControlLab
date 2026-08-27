using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LimsControlLab.Api.Auth;
using LimsControlLab.Api.Middleware;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Auditing;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.Domain.Services;
using LimsControlLab.Infrastructure;
using LimsControlLab.Infrastructure.Repositories;
using LimsControlLab.Infrastructure.Integration;
using LimsControlLab.Domain.Integration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod();
        if (builder.Environment.IsDevelopment())
        {
            // Local dev frontend ports shift around (ng serve auto-increments when
            // one is busy) - allow any localhost origin rather than an exact list.
            policy.SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Host == "localhost" || uri.Host == "127.0.0.1"));
        }
        else
        {
            policy.WithOrigins(corsAllowedOrigins);
        }
    }));
// Accept either an Npgsql keyword string or a postgres:// URL (Neon hands out URLs).
var limsDbConnectionString = NormalizePostgresConnectionString(
        builder.Configuration.GetConnectionString("LimsDb"))
    ?? throw new InvalidOperationException("LimsDb connection string not configured");

builder.Services.AddDbContext<LimsDbContext>(options =>
    options.UseNpgsql(limsDbConnectionString));

builder.Services.AddHealthChecks()
    .AddNpgSql(limsDbConnectionString, name: "cane-db");

builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IAuthorizationHandler, SiteRoleAuthorizationHandler>();
builder.Services.AddScoped<ICurrentUser>(provider =>
{
    var context = provider.GetRequiredService<IHttpContextAccessor>();
    return new CurrentUser(context.HttpContext?.User ?? throw new InvalidOperationException("HttpContext not available"));
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
builder.Services.AddScoped<IAnalysisTemplateRepository, AnalysisTemplateRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IInstrumentRepository, InstrumentRepository>();
builder.Services.AddScoped<ICalibrationCurveRepository, CalibrationCurveRepository>();
builder.Services.AddScoped<ISearchRepository, SearchRepository>();
builder.Services.AddScoped<IIntegrationLogRepository, IntegrationLogRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<AnalysisExecutionService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<ResultComparisonService>();
builder.Services.AddScoped<AnalysisTemplateService>();
builder.Services.AddScoped<ScheduleService>();
builder.Services.AddScoped<ScheduleAdherenceService>();
builder.Services.AddScoped<InstrumentReadingService>();
builder.Services.AddScoped<ResultLockingService>();
builder.Services.AddScoped<CalibrationCurveService>();
builder.Services.AddScoped<SampleTransferService>();
builder.Services.AddScoped<IDatabankSink, IllustrativeDatabankSink>();
builder.Services.AddScoped<ISCADASink, IllustrativeSCADASink>();
builder.Services.AddScoped<DatabankIntegrationService>();
builder.Services.AddScoped<ScadaPushService>();
builder.Services.AddScoped<AuditTrailService>();
builder.Services.AddScoped<IntegrationMonitoringService>();

var jwtSecret = builder.Configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey not configured");
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = "LimsControlLab",
            ValidateAudience = true,
            ValidAudience = "LimsControlLab",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization(options =>
{
    AuthorizationPolicies.AddSiteRolePolicies(options);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<TimeProvider>(_ => TimeProvider.System);

builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    // Behind Render's proxy TLS terminates at the edge; trust the forwarded
    // scheme/host so HTTPS redirection sees the real request and does not loop.
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
    db.Database.Migrate();

    // Seed demo data on first boot in any non-test environment (idempotent - only
    // seeds when the database is empty). Integration tests use "*test*" databases
    // and provide their own data, so they are skipped.
    var connectionString = db.Database.GetConnectionString() ?? "";
    var isTestDatabase = connectionString.Contains("test", StringComparison.OrdinalIgnoreCase);

    if (!isTestDatabase)
    {
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
        await SeedData.SeedIfEmptyAsync(db, passwordHasher: pwd => hasher.HashPassword(null, pwd), ct: CancellationToken.None);
    }
}

app.MapOpenApi();
app.UseHttpsRedirection();
app.UseCorrelationId();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Neon (and most managed Postgres) hand out a postgres:// URL, but Npgsql wants a
// keyword connection string. Convert a URL to keyword form and require SSL; pass a
// string that is already keyword form through unchanged.
static string? NormalizePostgresConnectionString(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
        return raw;

    if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        && !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        return raw;

    var uri = new Uri(raw);
    var userInfo = uri.UserInfo.Split(':', 2);

    var builder = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
        SslMode = Npgsql.SslMode.Require,
    };

    return builder.ConnectionString;
}
