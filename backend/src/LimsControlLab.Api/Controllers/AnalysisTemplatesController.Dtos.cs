namespace LimsControlLab.Api.Controllers;

public sealed record CreateAnalysisTemplateRequest
{
    public required string Name { get; init; }
    public required string Site { get; init; }
    public string? TestConfiguration { get; init; }
    public string? CalculationDefinitions { get; init; }
    public string? ValidationRules { get; init; }
    public decimal? MinTolerance { get; init; }
    public decimal? MaxTolerance { get; init; }
}

public sealed record UpdateAnalysisTemplateRequest
{
    public required string Name { get; init; }
    public string? TestConfiguration { get; init; }
    public string? CalculationDefinitions { get; init; }
    public string? ValidationRules { get; init; }
    public decimal? MinTolerance { get; init; }
    public decimal? MaxTolerance { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record AnalysisTemplateDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Site { get; init; }
    public required int Version { get; init; }
    public required bool IsRetired { get; init; }
    public string? TestConfiguration { get; init; }
    public string? CalculationDefinitions { get; init; }
    public string? ValidationRules { get; init; }
    public decimal? MinTolerance { get; init; }
    public decimal? MaxTolerance { get; init; }
    public required string RowVersion { get; init; }
}
