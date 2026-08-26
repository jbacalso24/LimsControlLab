using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Infrastructure.Repositories;

public sealed class InstrumentRepository : IInstrumentRepository
{
    private readonly LimsDbContext _context;

    public InstrumentRepository(LimsDbContext context)
    {
        _context = context;
    }

    public async Task<Instrument?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Instruments.FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<List<Instrument>> ListBySiteAsync(Site site, CancellationToken ct = default)
    {
        return await _context.Instruments
            .Where(i => i.Site == site)
            .OrderBy(i => i.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Instrument instrument, CancellationToken ct = default)
    {
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> TryUpdateWithConcurrencyCheckAsync(Instrument instrument, byte[] expectedRowVersion, CancellationToken ct = default)
    {
        try
        {
            _context.Entry(instrument).Property(i => i.RowVersion).OriginalValue = expectedRowVersion;
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
