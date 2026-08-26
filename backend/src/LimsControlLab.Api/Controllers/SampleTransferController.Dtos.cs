namespace LimsControlLab.Api.Controllers;

public sealed record SampleDto
{
    public required int Id { get; init; }
    public required string Identifier { get; init; }
    public required string Site { get; init; }
    public required string CurrentSite { get; init; }
    public required int AnalysisTemplateId { get; init; }
    public required string Status { get; init; }
    public required byte[] RowVersion { get; init; }
}

public sealed record SampleTransferDto
{
    public required int Id { get; init; }
    public required string FromSite { get; init; }
    public required string ToSite { get; init; }
    public required DateTimeOffset TransferredAtUtc { get; init; }
    public required byte[] RowVersion { get; init; }
}

public sealed record TransferSampleRequest
{
    public required string ToSite { get; init; }
    public required byte[] RowVersion { get; init; }
}
