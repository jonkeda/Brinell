using NetTopologySuite.Geometries;

namespace Brinell.Samples.Sql.App.Data;

/// <summary>A place on the map: every awkward kind of column the SQL replay spike needs once.</summary>
public sealed class Place
{
    /// <summary>Made by the app (<see cref="Guid.NewGuid"/>), not the database: a value replay has to bind.</summary>
    public Guid Id { get; set; }

    /// <summary>Unique: a second place with the name is refused by the database.</summary>
    public required string Name { get; set; }

    public required string Kind { get; set; }

    /// <summary>A geography point (SRID 4326), read through the SQL Server provider's own types.</summary>
    public required Point Location { get; set; }

    public decimal Population { get; set; }

    /// <summary>From the app's <see cref="TimeProvider"/>.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
