using System.Text.Json;
using LimsControlLab.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LimsControlLab.Api.Tests.Integration;

public sealed class ObservabilityIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private const string TestDbName = "cane-db-test-observability";

    public async Task InitializeAsync()
    {
        using var scope = new ServiceCollection()
            .AddDbContext<LimsDbContext>(options =>
                options.UseSqlServer($"Server=localhost;Database={TestDbName};Trusted_Connection=True;TrustServerCertificate=True;"))
            .BuildServiceProvider()
            .CreateScope();

        var dbTemp = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        await dbTemp.Database.EnsureDeletedAsync();
        await dbTemp.Database.MigrateAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<LimsDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<LimsDbContext>(options =>
                        options.UseSqlServer($"Server=localhost;Database={TestDbName};Trusted_Connection=True;TrustServerCertificate=True;"));
                });
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LimsDbContext>();
        await db.Database.EnsureDeletedAsync();

        await _factory.DisposeAsync();
        _client.Dispose();
    }

    [Fact]
    public async Task HealthCheckEndpointReturnsHealthyWhenDatabaseIsReachable()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body);
    }

    [Fact]
    public async Task HealthCheckEndpointReportsCaneDbHealth()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(body);
        // Health endpoint includes status
    }

    [Fact]
    public async Task CorrelationIdIsGeneratedWhenNotProvided()
    {
        var response = await _client.GetAsync("/health");

        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var correlationIds));
        var correlationId = correlationIds?.FirstOrDefault();
        Assert.NotEmpty(correlationId!);
        Assert.True(Guid.TryParse(correlationId, out _), "Correlation ID should be a valid GUID");
    }

    [Fact]
    public async Task CorrelationIdIsPreservedFromRequestHeader()
    {
        const string testCorrelationId = "test-correlation-id-12345";
        _client.DefaultRequestHeaders.Add("X-Correlation-Id", testCorrelationId);

        var response = await _client.GetAsync("/health");

        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var correlationIds));
        var correlationId = correlationIds?.FirstOrDefault();
        Assert.Equal(testCorrelationId, correlationId);
    }

    [Fact]
    public async Task CorrelationIdIsIncludedInResponseHeader()
    {
        const string testCorrelationId = "error-test-correlation-id";

        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-Id", testCorrelationId);

        var response = await _client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var correlationIds));
        Assert.Equal(testCorrelationId, correlationIds?.FirstOrDefault());
    }
}
