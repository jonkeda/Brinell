using NetTopologySuite.Geometries;

namespace Bruinet.Samples.Sql.App.Data;

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

/// <summary>A visit to a place: a second table, read by the detail page only, so a change to it reaches only the tests that open a place.</summary>
public sealed class Visit
{
    public Guid Id { get; set; }

    public Guid PlaceId { get; set; }

    public DateTimeOffset At { get; set; }
}

/// <summary>A row of the view <c>dbo.KindCounts</c>: how many places of a kind.</summary>
public sealed class KindCount
{
    public required string Kind { get; set; }

    public int Places { get; set; }
}
