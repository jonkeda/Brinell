using Brinell.Maui.Testing;
using Brinell.Presenter.Uat.Tests.PageObjects;

namespace Brinell.Presenter.Uat.Tests.Runtime;

public sealed class PresenterFixture : MauiTestFixtureBase
{
    public PresenterFixture()
    {
        PresenterPage = new PresenterPage(Context);
    }

    public PresenterPage PresenterPage { get; }

    protected override string GetDefaultAppPath(string platform)
    {
        var solutionDirectory = FindSolutionDirectory();
        return platform.ToLowerInvariant() switch
        {
            "windows" => Path.Combine(
                solutionDirectory,
                "srcnew",
                "Brinell.Presenter",
                "bin",
                "Debug",
                "net10.0-windows10.0.19041.0",
                "win-x64",
                "Brinell.Presenter.exe"),
            _ => string.Empty
        };
    }
}
