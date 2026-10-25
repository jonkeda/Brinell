using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Brinell.Samples.Sql.App.Data;

/// <summary>The spike app's database.</summary>
public sealed class SpikeDbContext(DbContextOptions<SpikeDbContext> options) : DbContext(options)
{
    public DbSet<Place> Places => Set<Place>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var place = modelBuilder.Entity<Place>();
        place.Property(p => p.Id).ValueGeneratedNever();
        place.Property(p => p.Name).HasMaxLength(100);
        place.HasIndex(p => p.Name).IsUnique();
        place.Property(p => p.Kind).HasMaxLength(30);
        place.Property(p => p.Location).HasColumnType("geography");
        place.Property(p => p.Population).HasPrecision(12, 2);
    }
}

/// <summary>For <c>dotnet ef migrations</c>: the local default database.</summary>
public sealed class SpikeDbContextDesignFactory : IDesignTimeDbContextFactory<SpikeDbContext>
{
    public SpikeDbContext CreateDbContext(string[] args) => new(SpikeData.Options(SpikeData.DefaultConnection));
}
