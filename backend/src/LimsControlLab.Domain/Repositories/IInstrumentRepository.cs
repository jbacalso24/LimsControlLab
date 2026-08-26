using LimsControlLab.Domain.Entities;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Repositories;

public interface IInstrumentRepository
{
    Task<Instrument?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Instrument>> ListBySiteAsync(Site site, CancellationToken ct = default);
    Task AddAsync(Instrument instrument, CancellationToken ct = default);
    Task<bool> TryUpdateWithConcurrencyCheckAsync(Instrument instrument, byte[] expectedRowVersion, CancellationToken ct = default);
}
