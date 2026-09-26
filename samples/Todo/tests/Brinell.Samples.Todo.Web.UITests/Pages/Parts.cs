using Brinell.Html.Controls;
using Brinell.Html.Controls.Buttons;
using Brinell.Html.Controls.Display;

namespace Brinell.Samples.Todo.Web.UITests.Pages;

/// <summary>
/// The status component (web <c>TodoStatusView</c>): a container whose parts' ids repeat in every
/// row, so they are found within it. The web version of the MAUI <c>TodoStatus</c> ControlObject.
/// </summary>
public sealed class TodoStatus<TParent>(IHtmlScope<TParent> parent, string testId)
    : ContainerBase<TParent, TodoStatus<TParent>>(parent, Locator.ByDataTestId(testId))
    where TParent : IHtmlScope<TParent>
{
    public override TodoStatus<TParent> Self => this;

    /// <summary>The status name, as shown ("Open", "In progress", "Done", "Overdue").</summary>
    public LabelControl<TodoStatus<TParent>> Text => new(this, Locator.ByDataTestId("StatusText"));

    /// <summary>Next: cycles the status. Absent where the control is read-only.</summary>
    public ButtonControl<TodoStatus<TParent>> Next => new(this, Locator.ByDataTestId("StatusNext"));

    /// <summary>Asserts the status shown, waiting for it.</summary>
    public TParent AssertStatus(string expected, int? timeoutMs = null)
    {
        Text.AssertText(expected, timeoutMs: timeoutMs);
        return Parent;
    }
}

/// <summary>A card with a header: controls inside it are found within the card.</summary>
public sealed class SectionCard<TParent>(IHtmlScope<TParent> parent, string testId)
    : ContainerBase<TParent, SectionCard<TParent>>(parent, Locator.ByDataTestId(testId))
    where TParent : IHtmlScope<TParent>
{
    public override SectionCard<TParent> Self => this;

    public LabelControl<SectionCard<TParent>> Header => new(this, Locator.ByDataTestId("SectionHeader"));

    /// <summary>A value label inside the card, by its test id.</summary>
    public LabelControl<SectionCard<TParent>> Value(string testId) => new(this, Locator.ByDataTestId(testId));
}

/// <summary>
/// The web head's alert, shown over its page. It does not ask its page: the page is busy while
/// the command that opened the dialog waits for its answer (design 7.2, MAUI's A3).
/// </summary>
public sealed class TodoDialog<TParent>(IHtmlScope<TParent> parent)
    : ContainerBase<TParent, TodoDialog<TParent>>(parent, Locator.ByDataTestId("Dialog"))
    where TParent : IHtmlScope<TParent>
{
    public override TodoDialog<TParent> Self => this;

    protected override bool AsksParent => false;

    public LabelControl<TodoDialog<TParent>> Title => new(this, Locator.ByDataTestId("Dialog_Title"));

    public ButtonControl<TodoDialog<TParent>> Accept => new(this, Locator.ByDataTestId("Dialog_Accept"));

    public ButtonControl<TodoDialog<TParent>> Cancel => new(this, Locator.ByDataTestId("Dialog_Cancel"));
}
