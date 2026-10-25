using System.Runtime.CompilerServices;
using Brinell.Maui.Munit;
using Brinell.Maui.Testing;
using Brinell.Samples.Maui.App;

namespace Brinell.Maui.UITests;

/// <summary>
/// Points the suite's fixtures at the MUnit head before any is made: each fixture renders the
/// sample app's first page in memory, in the app's own <c>App</c> (its resources and styles),
/// where on a device it would launch the app.
/// </summary>
internal static class HeadSetup
{
    [ModuleInitializer]
    internal static void UseMunit()
        => MauiTestFixtureBase.InMemoryHead = fixture => fixture switch
        {
            MauiFixture => MunitHead.InMemory(
                _ => new NavigationPage(new Brinell.Samples.Maui.App.HubPage()),
                options => options.Application = _ => new App()),
            _ => throw new NotSupportedException(
                $"{fixture.GetType().Name} has no MUnit head yet: its app is not compiled headless (.my/mUnit/suite-count.md)."),
        };
}
