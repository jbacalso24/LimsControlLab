using LimsControlLab.Domain.Entities;

namespace LimsControlLab.Domain.Integration;

/// <summary>
/// Abstraction for pushing validated LIMS results to Databank (R51, R52, R57).
/// Real HTTP implementation deferred — current implementation is illustrative.
/// </summary>
public interface IDatabankSink
{
    /// <summary>
    /// Transmit a locked, complete analysis to Databank (R52).
    /// Must exclude C Molasses Exchange per R57.
    /// </summary>
    Task<bool> TransmitAnalysisAsync(Analysis analysis, AnalysisTemplate analysisTemplate, CancellationToken ct);
}
