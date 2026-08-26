using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Infrastructure.Repositories;

public sealed class AnalysisRepository : IAnalysisRepository
{
    private readonly LimsDbContext _context;

    public AnalysisRepository(LimsDbContext context)
    {
        _context = context;
    }

    public async Task<Analysis?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Analyses
            .Include(a => a.Readings)
            .Include(a => a.Exceptions)
            .Include(a => a.TemplateVersion)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<Sample?> GetSampleByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Samples.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<AnalysisTemplate?> GetTemplateByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.AnalysisTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task AddReadingAsync(Reading reading, CancellationToken ct = default)
    {
        _context.Readings.Add(reading);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddExceptionAsync(ExceptionRecord exception, CancellationToken ct = default)
    {
        _context.ExceptionRecords.Add(exception);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddSampleTransferAsync(SampleTransfer transfer, CancellationToken ct = default)
    {
        var sample = await _context.Samples.FindAsync(new object[] { transfer.SampleId }, cancellationToken: ct);
        if (sample != null)
            _context.Entry(sample).State = EntityState.Modified;

        _context.SampleTransfers.Add(transfer);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> TryAddSampleTransferAsync(SampleTransfer transfer, Sample sample, byte[] expectedRowVersion, CancellationToken ct = default)
    {
        try
        {
            var dbSample = await _context.Samples.FindAsync(new object[] { sample.Id }, cancellationToken: ct);
            if (dbSample == null)
                return false;

            dbSample.CurrentSite = sample.CurrentSite;
            _context.Entry(dbSample).Property(s => s.RowVersion).OriginalValue = expectedRowVersion;
            _context.SampleTransfers.Add(transfer);
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<ExceptionRecord?> GetExceptionByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.ExceptionRecords.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<bool> TryUpdateAnalysisWithConcurrencyCheckAsync(Analysis analysis, byte[] expectedRowVersion, CancellationToken ct = default)
    {
        try
        {
            _context.Entry(analysis).Property(a => a.RowVersion).OriginalValue = expectedRowVersion;
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<bool> TryUpdateExceptionWithConcurrencyCheckAsync(ExceptionRecord exception, byte[] expectedRowVersion, CancellationToken ct = default)
    {
        try
        {
            _context.Entry(exception).Property(e => e.RowVersion).OriginalValue = expectedRowVersion;
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<IEnumerable<Analysis>> GetAnalysesWithExceptionsAsync(CancellationToken ct = default)
    {
        return await _context.Analyses
            .Include(a => a.Exceptions)
            .Include(a => a.Readings)
            .Where(a => a.Exceptions.Any())
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Analysis>> GetAnalysesWithExceptionsBySiteAsync(Site site, CancellationToken ct = default)
    {
        return await _context.Analyses
            .Include(a => a.Sample)
            .Include(a => a.Exceptions)
            .Include(a => a.TemplateVersion)
            .Where(a => a.Sample != null && a.Sample.Site == site && a.Exceptions.Any())
            .ToListAsync(ct);
    }
}
