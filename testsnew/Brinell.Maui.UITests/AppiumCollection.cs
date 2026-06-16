namespace Brinell.Maui.UITests;

/// <summary>
/// Collection definition for sharing the MauiFixture across all UI test classes.
/// Using a collection fixture ensures only ONE app instance is launched for all tests.
/// </summary>
/// <remarks>
/// Without this, each test class with IClassFixture&lt;MauiFixture&gt; would create
/// its own fixture, launching multiple app instances.
/// 
/// Usage: Add [Collection("Maui")] attribute to test classes instead of 
/// implementing IClassFixture&lt;MauiFixture&gt;.
/// </remarks>
[CollectionDefinition("Maui")]
public class MauiCollection : ICollectionFixture<MauiFixture>
{
    // This class has no code - it's just a marker for xUnit
    // to know that MauiFixture should be shared across all
    // test classes in this collection.
}
