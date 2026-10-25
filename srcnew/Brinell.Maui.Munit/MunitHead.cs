using Brinell.Core.Configuration;
using Brinell.Maui.Interfaces;
using MUnit.Rendering;
using BrinellMauiTestContext = Brinell.Maui.Context.MauiTestContext;
using BrinellMauiTestContextOptions = Brinell.Maui.Context.MauiTestContextOptions;
using MauiElement = Microsoft.Maui.Controls.Element;

namespace Brinell.Maui.Munit;

/// <summary>The MUnit head: a Brinell test context over a page MUnit rendered.</summary>
public static class MunitHead
{
    /// <summary>
    /// Brinell's timeouts for the head: short, because nothing settles on the wall clock in memory;
    /// a wait that is not met at once is waiting for something that will not come.
    /// </summary>
    public static TimeoutSettings Timeouts => new() { DefaultWait = 500, PageLoad = 500, ElementFind = 500 };

    /// <summary>
    /// The options for a Brinell context on an app made in memory: a new MUnit context, the
    /// <paramref name="root"/> page rendered into it, and the driver over that. For
    /// <c>MauiTestFixtureBase.InMemoryHead</c>, which runs a device suite's fixtures unchanged.
    /// </summary>
    /// <param name="root">The app's first page, as its <c>App</c> would show it.</param>
    /// <param name="configure">The MUnit context's options: the app's <c>Application</c>, adapters.</param>
    /// <param name="timeouts">Brinell's timeouts; <see cref="Timeouts"/> by default.</param>
    public static BrinellMauiTestContextOptions InMemory(
        Func<IServiceProvider, Microsoft.Maui.Controls.Page> root,
        Action<MUnit.MauiTestContextOptions>? configure = null,
        TimeoutSettings? timeouts = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        var options = new MUnit.MauiTestContextOptions();
        configure?.Invoke(options);
        var context = new MUnit.MauiTestContext(options);
        var rendered = context.Render(root);
        return new BrinellMauiTestContextOptions
        {
            Driver = new MunitMauiDriver(rendered),
            Timeouts = timeouts ?? Timeouts,
            Name = "MUnit",
        };
    }

    /// <summary>
    /// A Brinell <see cref="IMauiTestContext"/> on <paramref name="rendered"/>, for the same page objects
    /// the device heads use: <c>new ButtonsTestPage(rendered.AsBrinellContext())</c>.
    /// </summary>
    public static IMauiTestContext AsBrinellContext(this IRenderedElement<MauiElement> rendered, TimeoutSettings? timeouts = null)
        => new BrinellMauiTestContext(new BrinellMauiTestContextOptions
        {
            Driver = new MunitMauiDriver(rendered),
            Timeouts = timeouts ?? Timeouts,
            Name = "MUnit",
        });
}
