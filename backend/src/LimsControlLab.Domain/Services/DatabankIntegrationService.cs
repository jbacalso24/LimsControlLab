using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Integration;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Services;

/// <summary>
/// Manages transmission of validated LIMS results to Databank (R51–R57).
/// Only locked, complete analyses are transmitted; C Molasses Exchange is excluded this release (R57).
/// </summary>
public sealed class DatabankIntegrationService
{
    private readonly IDatabankSink _sink;
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IIntegrationLogRepository _logRepository;
    private readonly TimeProvider _timeProvider;

    public DatabankIntegrationService(
        IDatabankSink sink,
        IAnalysisRepository analysisRepository,
        IIntegrationLogRepository logRepository,
        TimeProvider timeProvider)
    {
        _sink = sink;
        _analysisRepository = analysisRepository;
        _logRepository = logRepository;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Attempt transmission of a locked analysis to Databank (R52, R53, R57).
    /// Returns true on success; false or exception on failure.
    /// Failures are logged to IntegrationLogEntry for visibility and reprocessing (R53).
    /// </summary>
    public async Task<bool> TransmitAnalysisAsync(int analysisId, CancellationToken ct)
    {
        var analysis = await _analysisRepository.GetByIdAsync(analysisId, ct);
        if (analysis == null)
            return false;

        var template = await _analysisRepository.GetTemplateByIdAsync(analysis.TemplateId, ct);
        if (template == null)
            return false;

        // R52: Only valid/complete (locked) analyses are transmitted.
        if (!analysis.IsLocked || analysis.Status != LifecycleStatus.Completed)
            return false;

        // R57: C Molasses Exchange is retained in LIMS but not transmitted this release.
        if (IsCMolassesExchange(template.Name))
            return false;

        // Record attempt before transmission.
        var logEntry = new IntegrationLogEntry
        {
            TargetSystem = "Databank",
            AnalysisId = analysisId,
            Status = "Pending",
            AttemptedAtUtc = _timeProvider.GetUtcNow(),
            RetryCount = 0,
        };
        await _logRepository.AddAsync(logEntry, ct);

        try
        {
            var success = await _sink.TransmitAnalysisAsync(analysis, template, ct);
            if (success)
            {
                logEntry.Status = "Success";
                logEntry.CompletedAtUtc = _timeProvider.GetUtcNow();
            }
            else
            {
                logEntry.Status = "Failed";
                logEntry.ErrorMessage = "Sink returned false";
                logEntry.CompletedAtUtc = _timeProvider.GetUtcNow();
            }

            await _logRepository.UpdateAsync(logEntry, ct);
            return success;
        }
        catch (Exception ex)
        {
            logEntry.Status = "Failed";
            logEntry.ErrorMessage = ex.Message;
            logEntry.CompletedAtUtc = _timeProvider.GetUtcNow();
            await _logRepository.UpdateAsync(logEntry, ct);
            return false;
        }
    }

    private static bool IsCMolassesExchange(string templateName)
    {
        return templateName.Contains("C Molasses Exchange", StringComparison.OrdinalIgnoreCase)
            || templateName.Contains("C Molasses", StringComparison.OrdinalIgnoreCase);
    }
}
