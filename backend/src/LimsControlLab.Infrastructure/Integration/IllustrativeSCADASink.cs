using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Integration;

namespace LimsControlLab.Infrastructure.Integration;

/// <summary>
/// Illustrative SCADA sink for testing and non-production use.
/// Real IP21 implementation deferred — will be replaced with actual IP21 integration.
/// </summary>
public sealed class IllustrativeSCADASink : ISCADASink
{
    public Task<bool> PushAnalysisAsync(Analysis analysis, AnalysisTemplate analysisTemplate, CancellationToken ct)
    {
        // Illustrative: log the push intent, then return success.
        // Real implementation: IP21 REST API or direct database write, handle errors.
        return Task.FromResult(true);
    }
}
