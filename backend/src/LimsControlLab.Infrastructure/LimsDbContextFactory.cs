using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LimsControlLab.Infrastructure;

public sealed class LimsDbContextFactory : IDesignTimeDbContextFactory<LimsDbContext>
{
    public LimsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LimsDbContext>()
            .UseSqlServer("Server=localhost;Database=cane-db;Trusted_Connection=True;TrustServerCertificate=True;");

        return new LimsDbContext(optionsBuilder.Options);
    }
}
