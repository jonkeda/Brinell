using Microsoft.Maui.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUnit.Assertions;
using MUnit.Interaction;
using MUnit.Samples.App;
using MUnit.Samples.App.Pages;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace MUnit.Tests.MSTest;

/// <summary>A view and the Shell on MSTest, its tests run in parallel by method.</summary>
[TestClass]
public sealed class GreetingAndShellTests
{
    [TestMethod]
    public void Waving_shows_the_count()
    {
        using var ctx = new MauiTestContext();
        var view = ctx.Render<GreetingView>();

        view.Find<Button>("Wave").Click();

        view.Find<Label>("Waves").Should().BeVisible().And.HaveText("1 wave");
    }

    [TestMethod]
    public void Help_opens_from_home()
    {
        using var ctx = new MauiTestContext();
        var app = ctx.Render<AppShell>();

        app.Find<Button>("Help").Click();

        var help = app.Navigation.ShouldBeOn<HelpPage>();
        Assert.AreEqual("HelpPage", help.AutomationId);
        app.Find<Label>("HelpText").Should().HaveText("Ask a survivor");
    }
}
