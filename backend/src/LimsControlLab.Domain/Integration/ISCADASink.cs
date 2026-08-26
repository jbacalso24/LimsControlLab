using LimsControlLab.Domain.Entities;

namespace LimsControlLab.Domain.Integration;

/// <summary>
/// Abstraction for pushing validated LIMS results to SCADA/IP21 in near-real-time (R51, R54).
/// Real IP21 implementation deferred — current implementation is illustrative.
/// </summary>
public interface ISCADASink
{
    /// <summary>
    /// Push a locked analysis to SCADA for operational displays (near-real-time, R54).
    /// </summary>
    Task<bool> PushAnalysisAsync(Analysis analysis, AnalysisTemplate analysisTemplate, CancellationToken ct);
}
