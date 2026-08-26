using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Integration;
using LimsControlLab.Domain.Repositories;

namespace LimsControlLab.Domain.Services;

/// <summary>
/// Manages near-real-time transmission of validated results to SCADA/IP21 for operational displays (R51, R54).
/// </summary>
public sealed class ScadaPushService
{
    private readonly ISCADASink _sink;
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IIntegrationLogRepository _logRepository;
    private readonly TimeProvider _timeProvider;

    public ScadaPushService(
        ISCADASink sink,
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
    /// Push a locked analysis to SCADA in near-real-time (R51, R54).
    /// Failures are logged for visibility (R53).
    /// </summary>
    public async Task<bool> PushAnalysisAsync(int analysisId, CancellationToken ct)
    {
        var analysis = await _analysisRepository.GetByIdAsync(analysisId, ct);
        if (analysis == null || !analysis.IsLocked)
            return false;

        var template = await _analysisRepository.GetTemplateByIdAsync(analysis.TemplateId, ct);
        if (template == null)
            return false;

        var logEntry = new IntegrationLogEntry
        {
            TargetSystem = "SCADA",
            AnalysisId = analysisId,
            Status = "Pending",
            AttemptedAtUtc = _timeProvider.GetUtcNow(),
            RetryCount = 0,
        };
        await _logRepository.AddAsync(logEntry, ct);

        try
        {
            var success = await _sink.PushAnalysisAsync(analysis, template, ct);
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
}
