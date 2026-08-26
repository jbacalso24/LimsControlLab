namespace LimsControlLab.Domain.Entities;

public sealed class ExceptionRecord
{
    public int Id { get; set; }
    public required int AnalysisId { get; set; }
    public required int ReadingId { get; set; }
    public required string Reason { get; set; }
    public string? Decision { get; set; }
    public string? DecisionComment { get; set; }
    public int? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Analysis? Analysis { get; set; }
    public Reading? Reading { get; set; }
}
