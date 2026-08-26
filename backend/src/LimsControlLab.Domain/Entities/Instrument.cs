using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Entities;

public sealed class Instrument
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public required Site Site { get; set; }
    public required bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
