using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Entities;

/// <summary>
/// Defines sampling method types: snap, composite, combined, split, exchange (R6).
/// </summary>
public sealed class SamplingMethod
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required Site Site { get; set; }
    public required bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
