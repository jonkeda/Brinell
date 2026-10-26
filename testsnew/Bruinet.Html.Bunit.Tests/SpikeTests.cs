using System.Diagnostics;
using AngleSharp.Dom;
using Bruinet.Html.Bunit.Tests.Components;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Bruinet.Html.Bunit.Tests;

/// <summary>The four questions of the bUnit head's spike (.my/bunit/plan.md step 1).</summary>
public sealed class SpikeTests(ITestOutputHelper output)
{
    private static BunitTestContext Probe() => new(new BunitTestContextOptions { RootComponent = typeof(Probe) });

    [Fact]
    public void Click_HandlerAwaitsDelay_PollSeesTheNewText()
    {
        using var context = Probe();
        var page = new ProbePage(context);

        var clock = Stopwatch.StartNew();
        page.Slow.Click();
        var click = clock.ElapsedMilliseconds;
        page.Status.AssertText("Done");
        var done = clock.ElapsedMilliseconds;

        output.WriteLine($"Click returned after {click} ms; the status read Done after {done} ms.");
    }

    [Fact]
    public void Find_ThenRerender_ElementReadsTheNewText()
    {
        using var context = Probe();
        var count = context.FindElement(Core.Locators.Locator.ByCss("[data-testid='count']"));

        context.FindElement(Core.Locators.Locator.ByCss("[data-testid='inc']")).Click();

        Assert.Equal("Count: 1", count.Text);
    }

    // What bUnit itself does with a node held across a render: the driver finds again either way.
    [Fact]
    public void Bunit_NodeHeldAcrossRender_Reports()
    {
        using var bunit = new BunitContext();
        var cut = bunit.Render<Probe>();
        var held = cut.Find("[data-testid='count']");

        cut.Find("[data-testid='inc']").Click();

        var fresh = cut.Find("[data-testid='count']");
        output.WriteLine($"Held node: '{held.TextContent}', same object as a fresh find: {ReferenceEquals(held, fresh)}, fresh: '{fresh.TextContent}'.");
    }

    [Fact]
    public void Render_SampleCounterPage_HasItsButton()
    {
        using var context = new BunitTestContext(new BunitTestContextOptions
        {
            RootComponent = typeof(Samples.Blazor.App.Components.Routes),
            ConfigureServices = SampleServices,
            BaseUrl = "http://localhost/counter",
        });

        var button = context.FindElement(Core.Locators.Locator.ByCss("[data-testid='increment-btn']"));

        Assert.True(button.Visible);
        output.WriteLine($"{context.CurrentUrl}: {context.Markup.Length} characters of markup.");
    }

    [Fact]
    public void Calls_HundredClicksAndAsserts_Median()
    {
        using var context = Probe();
        var page = new ProbePage(context);
        var times = new List<double>(100);

        for (var i = 1; i <= 100; i++)
        {
            var clock = Stopwatch.StartNew();
            page.Increment.Click();
            page.Count.AssertText($"Count: {i}");
            times.Add(clock.Elapsed.TotalMilliseconds);
        }

        times.Sort();
        output.WriteLine($"Click + AssertText: median {times[50]:F2} ms, fastest {times[0]:F2} ms, slowest {times[^1]:F2} ms.");
    }

    internal static void SampleServices(IServiceCollection services)
    {
        services.AddScoped<Samples.Blazor.App.Services.IAuthService, Samples.Blazor.App.Services.AuthService>();
        services.AddSingleton<Samples.Blazor.App.Services.DataService>();
        services.AddSingleton<Samples.Blazor.App.Services.MediaService>();
        services.AddSingleton<Samples.Blazor.App.Services.ToastService>();
    }
}
