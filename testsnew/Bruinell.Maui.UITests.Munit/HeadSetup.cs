using System.Runtime.CompilerServices;
using Bruinell.Maui.Munit;
using Bruinell.Maui.Testing;
using HubApp = Bruinell.Samples.Maui.App.App;
using HubPage = Bruinell.Samples.Maui.App.HubPage;
using ShellApp = Bruinell.Samples.Maui.ShellApp.App;
using ShellAppShell = Bruinell.Samples.Maui.ShellApp.AppShell;

namespace Bruinell.Maui.UITests;

/// <summary>
/// Points the suite's fixtures at the MUnit head before any is made: each fixture renders its
/// sample app's first page in memory, in the app's own <c>App</c> (its resources and styles), where
/// on a device it would launch the app.
/// </summary>
internal static class HeadSetup
{
    [ModuleInitializer]
    internal static void UseMunit()
        => MauiTestFixtureBase.InMemoryHead = fixture => fixture switch
        {
            MauiFixture => MunitHead.InMemory(
                _ => new NavigationPage(new HubPage()),
                options => options.Application = _ => new HubApp()),
            ShellFixture => MunitHead.InMemory(
                _ => new ShellAppShell(),
                options => options.Application = _ => new ShellApp()),
            _ => throw new NotSupportedException($"{fixture.GetType().Name} has no MUnit head: its app is not compiled headless."),
        };
}
