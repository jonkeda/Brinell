using Bruinet.Core.Configuration;
using Bruinet.Maui.Interfaces;
using MauiUnit.Rendering;
using BruinetMauiTestContext = Bruinet.Maui.Context.MauiTestContext;
using BruinetMauiTestContextOptions = Bruinet.Maui.Context.MauiTestContextOptions;
using MauiElement = Microsoft.Maui.Controls.Element;

namespace Bruinet.Maui.MauiUnit;

/// <summary>The MauiUnit head: a Bruinet test context over a page MauiUnit rendered.</summary>
public static class MauiUnitHead
{
    /// <summary>
    /// Bruinet's timeouts for the head: the device fixture's budgets, because an app in memory still
    /// waits on what is real (a backend answering after two seconds, a database), with no settling
    /// time for animations, which finish at once, and a shorter poll. A wait met at once costs nothing.
    /// </summary>
    public static TimeoutSettings Timeouts => new()
    {
        DefaultWait = 5000,
        PageLoad = 10000,
        ElementFind = 3000,
        ElementState = 3000,
        Animation = 0,
        PollingInterval = 25,
    };

    /// <summary>
    /// The options for a Bruinet context on an app made in memory: a new MauiUnit context, the
    /// <paramref name="root"/> page rendered into it, and the driver over that. For
    /// a fixture's <c>CreateInMemoryOptions</c>, which runs a device suite on the MauiUnit head (<c>BRUINET_HEAD=mauiunit</c>).
    /// </summary>
    /// <param name="root">The app's first page, as its <c>App</c> would show it.</param>
    /// <param name="configure">The MauiUnit context's options: the app's <c>Application</c>, adapters.</param>
    /// <param name="arrange">
    /// The app's services, registered on the new context before anything renders: what its
    /// <c>MauiProgram</c> registers, with the context's dispatcher where the app's needs a main thread.
    /// </param>
    /// <param name="timeouts">Bruinet's timeouts; <see cref="Timeouts"/> by default.</param>
    public static BruinetMauiTestContextOptions InMemory(
        Func<IServiceProvider, Microsoft.Maui.Controls.Page> root,
        Action<global::MauiUnit.MauiTestContextOptions>? configure = null,
        Action<global::MauiUnit.MauiTestContext>? arrange = null,
        TimeoutSettings? timeouts = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        var options = new global::MauiUnit.MauiTestContextOptions();
        configure?.Invoke(options);
        var context = new global::MauiUnit.MauiTestContext(options);
        arrange?.Invoke(context);
        var rendered = context.Render(root);
        return new BruinetMauiTestContextOptions
        {
            Driver = new MauiUnitDriver(rendered, ownsContext: true),
            OwnsDriver = true,
            Timeouts = timeouts ?? Timeouts,
            Name = "MauiUnit",
        };
    }

    /// <summary>
    /// A Bruinet <see cref="IMauiTestContext"/> on <paramref name="rendered"/>, for the same page objects
    /// the device heads use: <c>new ButtonsTestPage(rendered.AsBruinetContext())</c>.
    /// </summary>
    public static IMauiTestContext AsBruinetContext(this IRenderedElement<MauiElement> rendered, TimeoutSettings? timeouts = null)
        => new BruinetMauiTestContext(new BruinetMauiTestContextOptions
        {
            Driver = new MauiUnitDriver(rendered),
            Timeouts = timeouts ?? Timeouts,
            Name = "MauiUnit",
        });
}
