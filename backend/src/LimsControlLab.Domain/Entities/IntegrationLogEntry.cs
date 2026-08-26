namespace LimsControlLab.Domain.Entities;

/// <summary>
/// Records integration attempts to Databank, SCADA, and Data Lakehouse (R53).
/// Supports visibility and reprocessing on failure, separate from audit trail.
/// </summary>
public sealed class IntegrationLogEntry
{
    public int Id { get; set; }
    public required string TargetSystem { get; set; } // "Databank", "SCADA", "DataLakehouse"
    public required int AnalysisId { get; set; }
    public required string Status { get; set; } // "Pending", "Success", "Failed"
    public required DateTimeOffset AttemptedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    public Analysis? Analysis { get; set; }
}
