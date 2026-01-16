namespace Brinell.Maui.UITests;

/// <summary>
/// Collection definition for sharing the AppiumFixture across all UI test classes.
/// Using a collection fixture ensures only ONE app instance is launched for all tests.
/// </summary>
/// <remarks>
/// Without this, each test class with IClassFixture&lt;AppiumFixture&gt; would create
/// its own fixture, launching multiple app instances.
/// 
/// Usage: Add [Collection("Appium")] attribute to test classes instead of 
/// implementing IClassFixture&lt;AppiumFixture&gt;.
/// </remarks>
[CollectionDefinition("Appium")]
public class AppiumCollection : ICollectionFixture<AppiumFixture>
{
    // This class has no code - it's just a marker for xUnit
    // to know that AppiumFixture should be shared across all
    // test classes in this collection.
}
