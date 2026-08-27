using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Repositories;

namespace LimsControlLab.Domain.Services;

/// <summary>
/// Read + reprocess service for outbound integration attempts (R53).
/// Lists logged attempts to Databank / SCADA / DataLakehouse and re-attempts a failed
/// transmission by routing to the target system's transmission service. A re-attempt
/// records a fresh IntegrationLogEntry via the underlying service (visible on reload).
/// </summary>
public sealed class IntegrationMonitoringService
{
    private readonly IIntegrationLogRepository _repository;
    private readonly DatabankIntegrationService _databank;
    private readonly ScadaPushService _scada;

    public IntegrationMonitoringService(
        IIntegrationLogRepository repository,
        DatabankIntegrationService databank,
        ScadaPushService scada)
    {
        _repository = repository;
        _databank = databank;
        _scada = scada;
    }

    public async Task<Outcome<List<IntegrationLogItem>>> ListAsync(string? status, string? targetSystem, CancellationToken ct)
    {
        var entries = await _repository.ListAsync(status, targetSystem, ct);

        var items = entries.Select(e => new IntegrationLogItem
        {
            Id = e.Id,
            TargetSystem = e.TargetSystem,
            AnalysisId = e.AnalysisId,
            Status = e.Status,
            AttemptedAtUtc = e.AttemptedAtUtc,
            CompletedAtUtc = e.CompletedAtUtc,
            ErrorMessage = e.ErrorMessage,
            RetryCount = e.RetryCount,
        }).ToList();

        return new Outcome<List<IntegrationLogItem>>.Ok(items);
    }

    public async Task<Outcome<ReprocessResult>> ReprocessAsync(int logId, CancellationToken ct)
    {
        var entry = await _repository.GetByIdAsync(logId, ct);
        if (entry == null)
            return new Outcome<ReprocessResult>.NotFound($"Integration log {logId} not found.");

        bool success;
        switch (entry.TargetSystem)
        {
            case "Databank":
                success = await _databank.TransmitAnalysisAsync(entry.AnalysisId, ct);
                break;
            case "SCADA":
                success = await _scada.PushAnalysisAsync(entry.AnalysisId, ct);
                break;
            default:
                return new Outcome<ReprocessResult>.Invalid(
                    "targetSystem",
                    $"Reprocessing is not supported for {entry.TargetSystem}.");
        }

        return new Outcome<ReprocessResult>.Ok(new ReprocessResult
        {
            Id = entry.Id,
            Success = success,
            Status = success ? "Success" : "Failed",
            Message = success
                ? $"Re-attempt to {entry.TargetSystem} succeeded."
                : $"Re-attempt to {entry.TargetSystem} failed. Check the latest attempt for details.",
        });
    }
}

public sealed record IntegrationLogItem
{
    public required int Id { get; init; }
    public required string TargetSystem { get; init; }
    public required int AnalysisId { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset AttemptedAtUtc { get; init; }
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public string? ErrorMessage { get; init; }
    public required int RetryCount { get; init; }
}

public sealed record ReprocessResult
{
    public required int Id { get; init; }
    public required bool Success { get; init; }
    public required string Status { get; init; }
    public required string Message { get; init; }
}
