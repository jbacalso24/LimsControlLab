using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Integration;

namespace LimsControlLab.Infrastructure.Integration;

/// <summary>
/// Illustrative Databank sink for testing and non-production use.
/// Real HTTP implementation deferred — will be replaced with actual Databank API client.
/// </summary>
public sealed class IllustrativeDatabankSink : IDatabankSink
{
    public Task<bool> TransmitAnalysisAsync(Analysis analysis, AnalysisTemplate analysisTemplate, CancellationToken ct)
    {
        // Illustrative: log the transmission intent, then return success.
        // Real implementation: POST to Databank API, handle errors, retry.
        return Task.FromResult(true);
    }
}
