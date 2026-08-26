using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Auditing;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Services;

public sealed class AnalysisTemplateService
{
    private readonly IAnalysisTemplateRepository _templateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly TimeProvider _clock;

    public AnalysisTemplateService(
        IAnalysisTemplateRepository templateRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IAuditLogger auditLogger,
        TimeProvider clock)
    {
        _templateRepository = templateRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _clock = clock;
    }

    public async Task<Outcome<AnalysisTemplateServiceDto>> CreateAsync(
        CreateTemplateRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<AnalysisTemplateServiceDto>.Forbidden("Only Lab Coordinators can create templates.");

        var now = _clock.GetUtcNow();

        var template = new AnalysisTemplate
        {
            Name = request.Name,
            Site = request.Site,
            IsRetired = false,
        };

        _templateRepository.Add(template);
        await _unitOfWork.SaveChangesAsync(ct);

        var version = new AnalysisTemplateVersion
        {
            TemplateId = template.Id,
            Version = 1,
            TestConfiguration = request.TestConfiguration,
            CalculationDefinitions = request.CalculationDefinitions,
            ValidationRules = request.ValidationRules,
            MinTolerance = request.MinTolerance,
            MaxTolerance = request.MaxTolerance,
            CreatedAtUtc = now,
        };

        _templateRepository.AddVersion(version);
        await _unitOfWork.SaveChangesAsync(ct);

        template.CurrentVersionId = version.Id;
        _templateRepository.Update(template);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = now,
            Action = "CreateTemplate",
            EntityType = nameof(AnalysisTemplate),
            EntityId = template.Id,
            AfterValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                template.Name,
                template.Site,
                template.IsRetired,
            }),
        }, ct);

        return new Outcome<AnalysisTemplateServiceDto>.Ok(new AnalysisTemplateServiceDto
        {
            Id = template.Id,
            Name = template.Name,
            Site = template.Site.ToString(),
            Version = version.Version,
            IsRetired = template.IsRetired,
            TestConfiguration = version.TestConfiguration,
            CalculationDefinitions = version.CalculationDefinitions,
            ValidationRules = version.ValidationRules,
            MinTolerance = version.MinTolerance,
            MaxTolerance = version.MaxTolerance,
            RowVersion = template.RowVersion,
        });
    }

    public async Task<Outcome<AnalysisTemplateServiceDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var template = await _templateRepository.GetByIdAsync(id, ct);

        if (template == null || template.CurrentVersion == null)
            return new Outcome<AnalysisTemplateServiceDto>.NotFound($"Template {id} not found.");

        return new Outcome<AnalysisTemplateServiceDto>.Ok(new AnalysisTemplateServiceDto
        {
            Id = template.Id,
            Name = template.Name,
            Site = template.Site.ToString(),
            Version = template.CurrentVersion.Version,
            IsRetired = template.IsRetired,
            TestConfiguration = template.CurrentVersion.TestConfiguration,
            CalculationDefinitions = template.CurrentVersion.CalculationDefinitions,
            ValidationRules = template.CurrentVersion.ValidationRules,
            MinTolerance = template.CurrentVersion.MinTolerance,
            MaxTolerance = template.CurrentVersion.MaxTolerance,
            RowVersion = template.RowVersion,
        });
    }

    public async Task<Outcome<AnalysisTemplateServiceDto>> UpdateAsync(
        int id,
        UpdateTemplateRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<AnalysisTemplateServiceDto>.Forbidden("Only Lab Coordinators can create templates.");

        var template = await _templateRepository.GetByIdAsync(id, ct);

        if (template == null)
            return new Outcome<AnalysisTemplateServiceDto>.NotFound($"Template {id} not found.");

        if (!template.RowVersion.SequenceEqual(request.RowVersion))
            return new Outcome<AnalysisTemplateServiceDto>.Conflict(
                "Template was modified by another user. Please reload and try again.",
                Convert.ToBase64String(template.RowVersion));

        var now = _clock.GetUtcNow();
        var nextVersion = (template.CurrentVersion?.Version ?? 0) + 1;

        var newVersion = new AnalysisTemplateVersion
        {
            TemplateId = template.Id,
            Version = nextVersion,
            TestConfiguration = request.TestConfiguration,
            CalculationDefinitions = request.CalculationDefinitions,
            ValidationRules = request.ValidationRules,
            MinTolerance = request.MinTolerance,
            MaxTolerance = request.MaxTolerance,
            CreatedAtUtc = now,
        };

        _templateRepository.AddVersion(newVersion);
        await _unitOfWork.SaveChangesAsync(ct);

        template.Name = request.Name;
        template.CurrentVersionId = newVersion.Id;
        _templateRepository.Update(template);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = now,
            Action = "UpdateTemplate",
            EntityType = nameof(AnalysisTemplate),
            EntityId = template.Id,
            BeforeValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                template.CurrentVersion?.Version,
            }),
            AfterValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                Version = nextVersion,
            }),
        }, ct);

        return new Outcome<AnalysisTemplateServiceDto>.Ok(new AnalysisTemplateServiceDto
        {
            Id = template.Id,
            Name = template.Name,
            Site = template.Site.ToString(),
            Version = nextVersion,
            IsRetired = template.IsRetired,
            TestConfiguration = newVersion.TestConfiguration,
            CalculationDefinitions = newVersion.CalculationDefinitions,
            ValidationRules = newVersion.ValidationRules,
            MinTolerance = newVersion.MinTolerance,
            MaxTolerance = newVersion.MaxTolerance,
            RowVersion = template.RowVersion,
        });
    }

    public async Task<Outcome<bool>> RetireAsync(int id, CancellationToken ct)
    {
        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<bool>.Forbidden("Only Lab Coordinators can retire templates.");

        var template = await _templateRepository.GetByIdAsync(id, ct);

        if (template == null)
            return new Outcome<bool>.NotFound($"Template {id} not found.");

        if (template.IsRetired)
            return new Outcome<bool>.Invalid("templateId", "Template is already retired.");

        var now = _clock.GetUtcNow();
        template.IsRetired = true;
        _templateRepository.Update(template);

        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = now,
            Action = "RetireTemplate",
            EntityType = nameof(AnalysisTemplate),
            EntityId = template.Id,
            AfterValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                IsRetired = template.IsRetired,
            }),
        }, ct);

        return new Outcome<bool>.Ok(true);
    }

    public async Task<Outcome<List<AnalysisTemplateServiceDto>>> ListAsync(Site site, CancellationToken ct)
    {
        var templates = await _templateRepository.ListBySiteAsync(site, ct);

        var dtos = templates
            .Where(t => t.CurrentVersion != null)
            .Select(t => new AnalysisTemplateServiceDto
            {
                Id = t.Id,
                Name = t.Name,
                Site = t.Site.ToString(),
                Version = t.CurrentVersion!.Version,
                IsRetired = t.IsRetired,
                TestConfiguration = t.CurrentVersion.TestConfiguration,
                CalculationDefinitions = t.CurrentVersion.CalculationDefinitions,
                ValidationRules = t.CurrentVersion.ValidationRules,
                MinTolerance = t.CurrentVersion.MinTolerance,
                MaxTolerance = t.CurrentVersion.MaxTolerance,
                RowVersion = t.RowVersion,
            })
            .ToList();

        return new Outcome<List<AnalysisTemplateServiceDto>>.Ok(dtos);
    }
}

public sealed record CreateTemplateRequest
{
    public required Site Site { get; init; }
    public required string Name { get; init; }
    public string? TestConfiguration { get; init; }
    public string? CalculationDefinitions { get; init; }
    public string? ValidationRules { get; init; }
    public decimal? MinTolerance { get; init; }
    public decimal? MaxTolerance { get; init; }
}

public sealed record UpdateTemplateRequest
{
    public required string Name { get; init; }
    public string? TestConfiguration { get; init; }
    public string? CalculationDefinitions { get; init; }
    public string? ValidationRules { get; init; }
    public decimal? MinTolerance { get; init; }
    public decimal? MaxTolerance { get; init; }
    public required byte[] RowVersion { get; init; }
}

public sealed record AnalysisTemplateServiceDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Site { get; init; }
    public required int Version { get; init; }
    public required bool IsRetired { get; init; }
    public string? TestConfiguration { get; init; }
    public string? CalculationDefinitions { get; init; }
    public string? ValidationRules { get; init; }
    public decimal? MinTolerance { get; init; }
    public decimal? MaxTolerance { get; init; }
    public required byte[] RowVersion { get; init; }
}
