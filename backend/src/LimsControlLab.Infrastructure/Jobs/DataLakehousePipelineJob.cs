using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Infrastructure.Jobs;

/// <summary>
/// Batch feed of locked LIMS analyses to Data Lakehouse (R51, R54).
/// Runs periodically (schedule TBD during solution design).
/// Real file/message-queue drop mechanism deferred — current implementation is illustrative.
/// </summary>
public sealed class DataLakehousePipelineJob
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly TimeProvider _timeProvider;

    public DataLakehousePipelineJob(
        IAnalysisRepository analysisRepository,
        TimeProvider timeProvider)
    {
        _analysisRepository = analysisRepository;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Query locked, completed analyses and prepare for batch transmission to Data Lakehouse.
    /// Illustrative — actual feed mechanism (file drop, message queue) deferred.
    /// </summary>
    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        // ponytail: batch query assumes a GetLockedAnalysesAsync method exists on the repository.
        // If it doesn't, add it; for now, a simple illustrative placeholder.
        var cutoff = _timeProvider.GetUtcNow().AddHours(-1);
        var analyses = await _analysisRepository.GetByIdAsync(0, ct); // Illustrative placeholder

        return analyses != null ? 1 : 0;
    }
}
