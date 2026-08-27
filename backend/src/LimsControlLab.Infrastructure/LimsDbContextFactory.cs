using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LimsControlLab.Infrastructure;

public sealed class LimsDbContextFactory : IDesignTimeDbContextFactory<LimsDbContext>
{
    public LimsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LimsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=cane-db;Username=lims;Password=lims_dev_pw");

        return new LimsDbContext(optionsBuilder.Options);
    }
}
