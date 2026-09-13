using Brinell.Core;
using Brinell.Maui.Configuration;
using Brinell.Maui.Containers;

namespace Brinell.Maui.Extensions.Controls.Generated;

/// <summary>
/// MAUI generated editable field wrapper.
/// Handles generated field roots that expose child native buttons/text entries.
/// </summary>
/// <remarks>
/// <b>A container</b> (step 101a): its parts - the native buttons, the text entry - are looked for
/// under its own root and nowhere else. As a view it looked for them with <c>FindChildCore</c>,
/// which fell back to searching the whole page for any visible match whose centre lay inside the
/// field's bounds - a geometric guess that could as easily land on an overlapping neighbour.
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class EditableField<TScope> : ContainerObjectBase<TScope, EditableField<TScope>>
    where TScope : IMauiScope<TScope>
{
    private const string NativeButtonId = "EditableFieldView_NativeButton";
    private const string ButtonId = "EditableFieldView_Button";
    private const string TextEntryId = "EditableFieldView_TextEntry";
    private const string TextEditorButtonId = "EditableFieldView_TextEditorButton";
    private const string TextEditorNativeButtonId = "EditableFieldView_TextEditorNativeButton";
    private const string TextEditorId = "TextEditorView_Editor";
    private const string TextEditorOkButtonId = "IconButton_btnIcon";
    private const string TextEditorOkNativeButtonId = "IconButton_NativeButton";

    /// <summary>
    /// Creates a generated editable field control within the specified scope.
    /// </summary>
    public EditableField(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a generated editable field control within the specified scope using a string locator value.
    /// </summary>
    public EditableField(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    /// <summary>
    /// Locates the editor opened for a multiline generated field.
    /// </summary>
    protected virtual Locator TextEditorLocator => Locator.ByAutomationId(TextEditorId);

    /// <summary>
    /// Opens the field editor or picker.
    /// </summary>
    public TScope Open(int? timeoutMs = null)
    {
        if (!TryOpen(timeoutMs))
        {
            throw new ElementNotFoundException($"Could not open generated editable field. Locator: {Locator}");
        }

        return Parent;
    }

    /// <summary>
    /// Attempts to open the field editor or picker.
    /// </summary>
    public bool TryOpen(int? timeoutMs = null)
    {
        return Once(() =>
        {
            var root = TryGetContainerRoot();
            if (root == null) return false;

            var target = FindPart(NativeButtonId)
                ?? FindPart(TextEditorNativeButtonId)
                ?? FindPart(ButtonId)
                ?? FindPart(TextEditorButtonId)
                ?? root;

            return TryActivate(target);
        }, timeoutMs);
    }

    /// <summary>
    /// Sets the text entry value inside the generated field.
    /// </summary>
    public TScope SetText(string text, int? timeoutMs = null)
    {
        if (!TrySetText(text, timeoutMs))
        {
            throw new ElementNotFoundException($"Could not set generated editable field text. Locator: {Locator}");
        }

        return Parent;
    }

    /// <summary>
    /// Attempts to set the text entry value inside the generated field.
    /// </summary>
    public bool TrySetText(string text, int? timeoutMs = null)
    {
        return Once(() =>
        {
            var root = TryGetContainerRoot();
            if (root == null) return false;

            var entry = FindPart(TextEntryId);
            if (entry != null)
            {
                SetElementText(entry, text);
                return true;
            }

            return TrySetTextEditor(root, text, timeoutMs);
        }, timeoutMs);
    }

    /// <summary>
    /// Gets the generated field entry text when an entry child is available.
    /// </summary>
    public string? GetEntryText(int? timeoutMs = null)
    {
        return Once(() =>
        {
            var root = TryGetContainerRoot();
            return root == null ? null : FindPart(TextEntryId)?.Text;
        }, timeoutMs);
    }

    /// <summary>A visible part of this field, looked for under its root only.</summary>
    private IMauiElement? FindPart(string automationId)
        => FindElements(Locator.ByAutomationId(automationId)).FirstVisible();

    /// <summary>Runs an operation once, after the page is ready, and returns its result.</summary>
    private T Once<T>(Func<T> operation, int? timeoutMs)
    {
        var result = default(T)!;
        RunDo(() => result = operation(), timeoutMs);
        return result;
    }

    /// <summary>
    /// Activates one candidate surface of the generated field, reporting failure rather than throwing.
    /// </summary>
    /// <remarks>
    /// The template exposes several possible command surfaces (native button, icon, root) and
    /// callers try them in turn, so a failure here means "not this surface" and the caller
    /// falls back — to keyboard activation, for instance. A pointer-policy violation still
    /// surfaces: that is configuration, not a wrong candidate.
    /// </remarks>
    private static bool TryActivate(IMauiElement element)
    {
        if (!element.HasUsableBounds())
        {
            return false;
        }

        try
        {
            // One question, then one route - see GenericBrowser.TryActivate. No LegacyIAccessible
            // rung: DoDefaultAction was measured reporting success without doing anything.
            if (element.SupportsInvoke)
            {
                element.Invoke();
                return true;
            }

            element.Click();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TrySetTextEditor(IMauiElement root, string text, int? timeoutMs)
    {
        if (!TryOpenTextEditor(root, timeoutMs))
        {
            return false;
        }

        var editor = WaitForTextEditor(timeoutMs);
        if (editor == null)
        {
            return false;
        }

        SetElementText(editor, text);

        var okButton = WaitForTextEditorConfirmButton(timeoutMs);
        return okButton != null && TryActivate(okButton);
    }

    private bool TryOpenTextEditor(IMauiElement root, int? timeoutMs)
    {
        var target = FindPart(TextEditorNativeButtonId)
            ?? FindPart(TextEditorButtonId)
            ?? root;

        if (TryActivate(target) && WaitForTextEditorOpen(timeoutMs))
        {
            return true;
        }

        if (TryKeyboardActivate(root, Keys.Enter) && WaitForTextEditorOpen(timeoutMs))
        {
            return true;
        }

        return TryKeyboardActivate(root, Keys.Space) && WaitForTextEditorOpen(timeoutMs);
    }

    private static bool TryKeyboardActivate(IMauiElement element, string key)
    {
        try
        {
            element.SendKeys(key);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool WaitForTextEditorOpen(int? timeoutMs)
        => WaitForTextEditor(timeoutMs) != null
           && WaitForTextEditorConfirmButton(timeoutMs) != null;

    private IMauiElement? WaitForTextEditor(int? timeoutMs)
    {
        IMauiElement? result = null;
        RunWait(
            () => (result = Parent.FindVisibleElements(TextEditorLocator).FirstOrDefault()) != null,
            timeoutMs);
        return result;
    }

    private IMauiElement? WaitForTextEditorConfirmButton(int? timeoutMs)
    {
        IMauiElement? result = null;
        RunWait(
            () => (result = Parent.FindVisibleByAutomationId(TextEditorOkNativeButtonId)
                            ?? Parent.FindVisibleByAutomationId(TextEditorOkButtonId)) != null,
            timeoutMs);
        return result;
    }

    private static void SetElementText(IMauiElement element, string text)
    {
        element.Clear();
        element.SendKeys(text, TextInputMethod.SetValue);
    }
}
