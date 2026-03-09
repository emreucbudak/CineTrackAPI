using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CineTrack.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CineTrackDbContext>
{
    public CineTrackDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CineTrackDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=cinetrack;Username=postgres;Password=postgres");

        return new CineTrackDbContext(optionsBuilder.Options);
    }
}
