using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Infrastructure.Repositories;

public sealed class ScheduleRepository : IScheduleRepository
{
    private readonly LimsDbContext _db;

    public ScheduleRepository(LimsDbContext db)
    {
        _db = db;
    }

    public async Task<Schedule?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Schedules.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<List<Schedule>> ListBySiteAsync(Site site, CancellationToken ct = default)
    {
        return await _db.Schedules
            .Where(s => s.Site == site)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);
    }

    public void Add(Schedule schedule)
    {
        _db.Schedules.Add(schedule);
    }

    public void Update(Schedule schedule)
    {
        _db.Schedules.Update(schedule);
    }

    public void Remove(Schedule schedule)
    {
        _db.Schedules.Remove(schedule);
    }
}
