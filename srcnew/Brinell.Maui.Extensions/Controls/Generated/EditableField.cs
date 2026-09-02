using Brinell.Core;
using Brinell.Maui.Configuration;

namespace Brinell.Maui.Extensions.Controls.Generated;

/// <summary>
/// MAUI generated editable field wrapper.
/// Handles generated field roots that expose child native buttons/text entries.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class EditableField<TScope> : Brinell.Maui.Controls.Base.ViewBase<TScope>
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
    /// Opens the field editor or picker.
    /// </summary>
    public TScope Open(int? timeoutMs = null)
    {
        if (!TryOpen(timeoutMs))
        {
            throw new ElementNotFoundException($"Could not open generated editable field. Locator: {Locator}");
        }

        return ContainingScope;
    }

    /// <summary>
    /// Attempts to open the field editor or picker.
    /// </summary>
    public bool TryOpen(int? timeoutMs = null)
    {
        return Run(nameof(TryOpen), (string?)null, () =>
        {
            var root = TryFindElement();
            if (root == null) return false;

            var target = FindChild(root, NativeButtonId)
                ?? FindChild(root, TextEditorNativeButtonId)
                ?? FindChild(root, ButtonId)
                ?? FindChild(root, TextEditorButtonId)
                ?? root;

            return TryActivate(target);
        });
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

        return ContainingScope;
    }

    /// <summary>
    /// Attempts to set the text entry value inside the generated field.
    /// </summary>
    public bool TrySetText(string text, int? timeoutMs = null)
    {
        return Run(nameof(TrySetText), text, () =>
        {
            var root = TryFindElement();
            if (root == null) return false;

            var entry = FindChild(root, TextEntryId);
            if (entry != null)
            {
                SetElementText(entry, text);
                return true;
            }

            return TrySetTextEditor(root, text, timeoutMs);
        });
    }

    /// <summary>
    /// Gets the generated field entry text when an entry child is available.
    /// </summary>
    public string? GetEntryText(int? timeoutMs = null)
    {
        return Run(nameof(GetEntryText), (string?)null, () =>
        {
            var root = TryFindElement();
            return root == null ? null : FindChild(root, TextEntryId)?.Text;
        });
    }

    private IMauiElement? FindChild(IMauiElement root, string automationId)
        => FindChildCore(root, automationId);

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
            if (element is IInvokePatternElement { SupportsInvokePattern: true } invoke
                && invoke.InvokePattern())
            {
                return true;
            }

            if (element is ILegacyIAccessiblePatternElement { SupportsLegacyIAccessiblePattern: true } legacy
                && legacy.DoDefaultActionPattern())
            {
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
        var target = FindChild(root, TextEditorNativeButtonId)
            ?? FindChild(root, TextEditorButtonId)
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
        => WaitForAutomationId(TextEditorId, timeoutMs)
           ?? WaitForLargestEditControl(timeoutMs);

    private IMauiElement? WaitForTextEditorConfirmButton(int? timeoutMs)
        => WaitForAutomationId(TextEditorOkNativeButtonId, timeoutMs)
           ?? WaitForAutomationId(TextEditorOkButtonId, timeoutMs);

    private IMauiElement? WaitForLargestEditControl(int? timeoutMs)
    {
        IMauiElement? result = null;
        RunWait(
            () =>
            {
                result = MauiScope
                    .FindElements(Locator.ByControlType("Edit"))
                    .Where(element => element.HasUsableBounds())
                    .Where(element => !string.Equals(
                        element.GetAttribute("AutomationId"),
                        TextEntryId,
                        StringComparison.Ordinal))
                    .OrderByDescending(element => element.Rect.Width * element.Rect.Height)
                    .FirstOrDefault();
                return result != null;
            },
            timeoutMs);

        return result;
    }

    private IMauiElement? WaitForAutomationId(string automationId, int? timeoutMs)
    {
        IMauiElement? result = null;
        RunWait(() => (result = MauiScope.FindVisibleByAutomationId(automationId)) != null, timeoutMs);
        return result;
    }

    private static void SetElementText(IMauiElement element, string text)
    {
        element.Clear();
        element.SendKeys(text, TextInputMethod.SetValue);
    }
}
