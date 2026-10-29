using Microsoft.Maui.Controls;
using MauiHeadless.Assertions;
using MauiHeadless.Inspection;
using MauiHeadless.Snapshots;

namespace MauiHeadless.Rendering;

internal sealed class RenderedElement<T> : IRenderedElement<T> where T : Element
{
    private readonly Page _root;
    private bool _disposed;

    public RenderedElement(MauiTestContext context, T instance, Window window, Page root)
    {
        Context = context;
        Instance = instance;
        Window = window;
        _root = root;
        Navigation = new TestNavigator(context, window);
    }

    public MauiTestContext Context { get; }

    public T Instance { get; }

    public Window Window { get; }

    public Page CurrentPage
    {
        get
        {
            Context.Dispatcher.RunPending();
            return Screen.CurrentPage(Window) ?? _root;
        }
    }

    public TestNavigator Navigation { get; }

    public VisualNode Tree => ElementQueries.Read(Window);

    public TElement Find<TElement>(string? automationId = null) where TElement : Element
        => Window.Find<TElement>(automationId);

    public IReadOnlyList<TElement> FindAll<TElement>(string? automationId = null) where TElement : Element
        => Window.FindAll<TElement>(automationId);

    public Element FindByAutomationId(string automationId) => Window.FindByAutomationId(automationId);

    public TElement FindByText<TElement>(string text) where TElement : Element => Window.FindByText<TElement>(text);

    public string ToSnapshot(SnapshotOptions? options = null) => TreeSnapshot.Format(Tree, options);

    public void WaitForAssertion(Action assertion, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        var passed = Context.Dispatcher.WaitFor(
            () =>
            {
                try
                {
                    assertion();
                    return true;
                }
                catch (MauiHeadlessAssertionException)
                {
                    return false;
                }
                catch (ElementNotFoundException)
                {
                    return false;
                }
            },
            timeout ?? Context.Options.WaitTimeout);

        // The last try's failure is the one to show.
        if (!passed)
            assertion();
    }

    public void WaitForState(Func<bool> predicate, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var wait = timeout ?? Context.Options.WaitTimeout;
        if (!Context.Dispatcher.WaitFor(predicate, wait))
            throw new TimeoutException(
                $"The state was not reached within {wait.TotalMilliseconds:0} ms.\n\nScreen:\n{TreeSnapshot.Format(Tree, TreeSnapshot.ForMessages)}");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Context.Dispatcher.Run(() =>
        {
            _root.SendDisappearing();
            if (Window.Parent is Application application)
                Internals.MauiInternals.RemoveWindow(application, Window);
        });
    }
}
