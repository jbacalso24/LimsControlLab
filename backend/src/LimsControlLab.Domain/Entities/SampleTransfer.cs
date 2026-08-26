using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Entities;

public sealed class SampleTransfer
{
    public int Id { get; set; }
    public required int SampleId { get; set; }
    public required Site FromSite { get; set; }
    public required Site ToSite { get; set; }
    public required int TransferredByUserId { get; set; }
    public required DateTimeOffset TransferredAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Sample? Sample { get; set; }
}
