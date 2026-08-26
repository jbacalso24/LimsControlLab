namespace LimsControlLab.Domain.Entities;

public sealed class Reading
{
    public int Id { get; set; }
    public required int AnalysisId { get; set; }
    public required int TestId { get; set; }
    public required decimal Value { get; set; }
    public required string Unit { get; set; }
    public required DateTimeOffset CapturedAtUtc { get; set; }
    public required int CapturedByUserId { get; set; }
    public int? InstrumentId { get; set; }
    public required string ValidationResult { get; set; }
    public decimal? CalibratedValue { get; set; }

    public Analysis? Analysis { get; set; }
}
