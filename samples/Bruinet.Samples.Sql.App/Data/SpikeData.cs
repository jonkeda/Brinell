using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Bruinet.Samples.Sql.App.Data;

/// <summary>The app's data composition, which <c>Program.cs</c> and a test's bUnit head share.</summary>
public static class SpikeData
{
    /// <summary>The environment variable that names the database.</summary>
    public const string ConnectionVariable = "BRUINET_SQL_CONNECTION";

    /// <summary>The local SQL Server, never LocalDB.</summary>
    public const string DefaultConnection = "Server=.;Database=BruinetSqlSpike;Integrated Security=True;TrustServerCertificate=True";

    public static string Connection
        => Environment.GetEnvironmentVariable(ConnectionVariable) is { Length: > 0 } connection ? connection : DefaultConnection;

    /// <summary>Points in geography's SRID.</summary>
    public static GeometryFactory Geography { get; } = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public static DbContextOptionsBuilder UseSpikeSqlServer(this DbContextOptionsBuilder options, string connection)
        => options.UseSqlServer(connection, sql => sql.UseNetTopologySuite());

    /// <summary>Options for a context made by hand, such as a test fixture's for seeding.</summary>
    public static DbContextOptions<SpikeDbContext> Options(string connection)
    {
        var options = new DbContextOptionsBuilder<SpikeDbContext>();
        options.UseSpikeSqlServer(connection);
        return options.Options;
    }

    /// <summary>The context factory and the clock; <paramref name="configure"/> adds what a test needs, such as interceptors.</summary>
    public static IServiceCollection AddSpikeData(this IServiceCollection services, string connection, Action<DbContextOptionsBuilder>? configure = null)
    {
        services.AddDbContextFactory<SpikeDbContext>(options =>
        {
            options.UseSpikeSqlServer(connection);
            configure?.Invoke(options);
        });
        services.AddSingleton(TimeProvider.System);
        return services;
    }

    public static Point At(double latitude, double longitude) => Geography.CreatePoint(new Coordinate(longitude, latitude));
}

/// <summary>The five places a test starts from; written by the test fixture, never by the app.</summary>
public static class PlaceSeed
{
    /// <summary>The place distances are measured from.</summary>
    public const string Origin = "Hoorn";

    public static IReadOnlyList<Place> Places() =>
    [
        Make("a1000000-0000-0000-0000-000000000001", "Amsterdam", "city", 52.3676, 4.9041, 931298m),
        Make("a1000000-0000-0000-0000-000000000002", "Enkhuizen", "town", 52.7035, 5.2930, 18945m),
        Make("a1000000-0000-0000-0000-000000000003", Origin, "town", 52.6424, 5.0597, 74064m),
        Make("a1000000-0000-0000-0000-000000000004", "Medemblik", "town", 52.7720, 5.1066, 47402m),
        Make("a1000000-0000-0000-0000-000000000005", "Purmerend", "city", 52.5050, 4.9592, 93451m),
    ];

    /// <summary>The database migrated and holding the five places only, which it returns.</summary>
    public static async Task<IReadOnlyList<Place>> ResetAsync(SpikeDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);
        await db.Visits.ExecuteDeleteAsync(cancellationToken);
        await db.Places.ExecuteDeleteAsync(cancellationToken);
        var places = Places();
        db.Places.AddRange(places);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
        return places;
    }

    // Fixed ids: test data is the same every run, so only what the app makes differs between recordings.
    private static Place Make(string id, string name, string kind, double latitude, double longitude, decimal population) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        Kind = kind,
        Location = SpikeData.At(latitude, longitude),
        Population = population,
        CreatedAt = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.FromHours(2)),
    };
}
