using LimsControlLab.Domain.Auditing;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Services;

public sealed class ResultLockingService
{
    private readonly IAnalysisRepository _repository;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ResultLockingService(
        IAnalysisRepository repository,
        IAuditLogger auditLogger,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Outcome<ResultUnlockResult>> UnlockResultAsync(int analysisId, UnlockResultRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Justification))
            return new Outcome<ResultUnlockResult>.Invalid("justification", "Justification is mandatory when unlocking a result (R46, R47).");

        if (string.IsNullOrEmpty(request.RowVersion))
            return new Outcome<ResultUnlockResult>.Invalid("rowVersion", "Row version is required for concurrency control.");

        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<ResultUnlockResult>.Forbidden("Only Lab Coordinators can unlock results (R47).");

        var analysis = await _repository.GetByIdAsync(analysisId, ct);
        if (analysis == null)
            return new Outcome<ResultUnlockResult>.NotFound($"Analysis {analysisId} not found.");

        if (!analysis.IsLocked)
            return new Outcome<ResultUnlockResult>.Invalid("isLocked", "Analysis is not locked.");

        var beforeValues = $"IsLocked: {analysis.IsLocked}, LockedAtUtc: {analysis.LockedAtUtc}, LockedByUserId: {analysis.LockedByUserId}";

        analysis.IsLocked = false;
        analysis.LockedAtUtc = null;
        analysis.LockedByUserId = null;

        var expectedRowVersion = Convert.FromBase64String(request.RowVersion);
        var concurrencySuccess = await _repository.TryUpdateAnalysisWithConcurrencyCheckAsync(analysis, expectedRowVersion, ct);

        if (!concurrencySuccess)
        {
            var refreshedAnalysis = await _repository.GetByIdAsync(analysisId, ct);
            return new Outcome<ResultUnlockResult>.Conflict(
                $"Concurrent modification detected. Current row version is {Convert.ToBase64String(refreshedAnalysis!.RowVersion)}",
                Convert.ToBase64String(refreshedAnalysis!.RowVersion));
        }

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = _timeProvider.GetUtcNow(),
            Action = "ResultUnlocked",
            EntityType = "Analysis",
            EntityId = analysis.Id,
            BeforeValues = beforeValues,
            AfterValues = $"IsLocked: {analysis.IsLocked}, Justification: {request.Justification}",
        }, ct);

        var refreshed = await _repository.GetByIdAsync(analysisId, ct);
        var result = new ResultUnlockResult
        {
            Id = refreshed!.Id,
            IsLocked = refreshed.IsLocked,
            RowVersion = Convert.ToBase64String(refreshed.RowVersion),
        };

        return new Outcome<ResultUnlockResult>.Ok(result);
    }

    public async Task<Outcome<List<ExceptionAnalysisResult>>> GetExceptionAnalysesAsync(CancellationToken ct)
    {
        var analyses = await _repository.GetAnalysesWithExceptionsBySiteAsync(_currentUser.Site, ct);

        var results = analyses.Select(a => new ExceptionAnalysisResult
        {
            Id = a.Id,
            SampleId = a.SampleId,
            SampleIdentifier = a.Sample?.Identifier ?? $"#{a.SampleId}",
            TemplateId = a.TemplateId,
            TemplateName = a.Template?.Name ?? $"#{a.TemplateId}",
            Site = a.Sample?.Site.ToString() ?? "",
            Status = a.Status.ToString(),
            StartedAtUtc = a.StartedAtUtc,
            CompletedAtUtc = a.CompletedAtUtc,
            StartedByUserId = a.StartedByUserId,
            IsLocked = a.IsLocked,
            LockedAtUtc = a.LockedAtUtc,
            LockedByUserId = a.LockedByUserId,
            Exceptions = a.Exceptions.Select(e => new ExceptionDetail
            {
                Id = e.Id,
                ReadingId = e.ReadingId,
                Reason = e.Reason,
                Decision = e.Decision,
                DecisionComment = e.DecisionComment,
                DecidedByUserId = e.DecidedByUserId,
                DecidedAtUtc = e.DecidedAtUtc,
                RowVersion = Convert.ToBase64String(e.RowVersion),
            }).ToArray(),
            RowVersion = Convert.ToBase64String(a.RowVersion),
        }).ToList();

        return new Outcome<List<ExceptionAnalysisResult>>.Ok(results);
    }
}

public sealed record UnlockResultRequest
{
    public required string Justification { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record ResultUnlockResult
{
    public required int Id { get; init; }
    public required bool IsLocked { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record ExceptionAnalysisResult
{
    public required int Id { get; init; }
    public required int SampleId { get; init; }
    public required string SampleIdentifier { get; init; }
    public required int TemplateId { get; init; }
    public required string TemplateName { get; init; }
    public required string Site { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public required int StartedByUserId { get; init; }
    public required bool IsLocked { get; init; }
    public DateTimeOffset? LockedAtUtc { get; init; }
    public int? LockedByUserId { get; init; }
    public required ExceptionDetail[] Exceptions { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record ExceptionDetail
{
    public required int Id { get; init; }
    public required int ReadingId { get; init; }
    public required string Reason { get; init; }
    public string? Decision { get; init; }
    public string? DecisionComment { get; init; }
    public int? DecidedByUserId { get; init; }
    public DateTimeOffset? DecidedAtUtc { get; init; }
    public required string RowVersion { get; init; }
}
