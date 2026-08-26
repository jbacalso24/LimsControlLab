using LimsControlLab.Domain.Auditing;
using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Services;

public sealed class SampleTransferService
{
    private readonly IAnalysisRepository _repository;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SampleTransferService(
        IAnalysisRepository repository,
        IAuditLogger auditLogger,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Outcome<Sample>> GetByIdAsync(int sampleId, CancellationToken ct)
    {
        var sample = await _repository.GetSampleByIdAsync(sampleId, ct);
        if (sample == null)
            return new Outcome<Sample>.NotFound($"Sample {sampleId} not found.");

        if (_currentUser.Site != sample.Site && _currentUser.Site != sample.CurrentSite)
            return new Outcome<Sample>.Forbidden("You do not have access to view this sample.");

        return new Outcome<Sample>.Ok(sample);
    }

    public async Task<Outcome<SampleTransferResult>> TransferAsync(int sampleId, Site toSite, byte[] expectedRowVersion, CancellationToken ct)
    {
        var sample = await _repository.GetSampleByIdAsync(sampleId, ct);
        if (sample == null)
            return new Outcome<SampleTransferResult>.NotFound($"Sample {sampleId} not found.");

        if (_currentUser.Site != sample.CurrentSite)
            return new Outcome<SampleTransferResult>.Forbidden("Only the current site can transfer a sample.");

        if (sample.CurrentSite == toSite)
            return new Outcome<SampleTransferResult>.Invalid("toSite", "Cannot transfer to the same site.");

        var fromSite = sample.CurrentSite;
        sample.CurrentSite = toSite;

        var transfer = new SampleTransfer
        {
            SampleId = sampleId,
            FromSite = fromSite,
            ToSite = toSite,
            TransferredByUserId = _currentUser.UserId,
            TransferredAtUtc = _timeProvider.GetUtcNow(),
        };

        var concurrencyCheckPassed = await _repository.TryAddSampleTransferAsync(transfer, sample, expectedRowVersion, ct);
        if (!concurrencyCheckPassed)
            return new Outcome<SampleTransferResult>.Conflict("Sample was modified by another request.", Convert.ToBase64String(sample.RowVersion ?? Array.Empty<byte>()));

        await _auditLogger.LogAsync(new AuditLogEntryRecord
        {
            UserId = _currentUser.UserId,
            Role = _currentUser.Role.ToString(),
            TimestampUtc = _timeProvider.GetUtcNow(),
            Action = "SampleTransferred",
            EntityType = "Sample",
            EntityId = sampleId,
            BeforeValues = $"CurrentSite: {fromSite}",
            AfterValues = $"CurrentSite: {toSite}",
        }, ct);

        return new Outcome<SampleTransferResult>.Ok(new SampleTransferResult
        {
            Id = sampleId,
            FromSite = fromSite,
            ToSite = toSite,
            TransferredAtUtc = transfer.TransferredAtUtc,
            RowVersion = sample.RowVersion,
        });
    }
}

public sealed record SampleTransferResult
{
    public required int Id { get; init; }
    public required Site FromSite { get; init; }
    public required Site ToSite { get; init; }
    public required DateTimeOffset TransferredAtUtc { get; init; }
    public required byte[] RowVersion { get; init; }
}
