using Brinell.Core;

namespace Brinell.Maui.Extensions.Controls.Generated;

/// <summary>
/// MAUI generated editable field wrapper.
/// Handles generated field roots that expose child native buttons/text entries.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class EditableField<TScope> : ControlBase<TScope>
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
        return Run(nameof(TryOpen), () =>
        {
            var root = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
            var target = FindChild(root, NativeButtonId)
                ?? FindChild(root, TextEditorNativeButtonId)
                ?? FindChild(root, ButtonId)
                ?? FindChild(root, TextEditorButtonId)
                ?? root;

            return ElementActivator.TryActivate(target);
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
            var root = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
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
        return Run(nameof(GetEntryText), () =>
        {
            var root = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
            return FindChild(root, TextEntryId)?.Text;
        });
    }

    private IMauiElement? FindChild(IMauiElement root, string automationId)
        => ElementSearch.FindChildByAutomationId(MauiScope, root, automationId);

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
        return ElementActivator.TryActivate(okButton);
    }

    private bool TryOpenTextEditor(IMauiElement root, int? timeoutMs)
    {
        var target = FindChild(root, TextEditorNativeButtonId)
            ?? FindChild(root, TextEditorButtonId)
            ?? root;

        if (ElementActivator.TryActivate(target) && WaitForTextEditorOpen(timeoutMs))
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
        catch (WindowsInteractionPolicyException)
        {
            throw;
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
        ElementSearch.WaitUntil(
            () =>
            {
                result = MauiScope
                    .FindElements(Locator.ByControlType("Edit"))
                    .Where(ElementSearch.HasUsableBounds)
                    .Where(element => !string.Equals(
                        element.GetAttribute("AutomationId"),
                        TextEntryId,
                        StringComparison.Ordinal))
                    .OrderByDescending(element => element.Rect.Width * element.Rect.Height)
                    .FirstOrDefault();
                return result != null;
            },
            TimeSpan.FromMilliseconds(timeoutMs ?? DefaultTimeoutMs));

        return result;
    }

    private IMauiElement? WaitForAutomationId(string automationId, int? timeoutMs)
    {
        IMauiElement? result = null;
        ElementSearch.WaitUntil(
            () =>
            {
                result = ElementSearch.FindVisibleByAutomationId(MauiScope, automationId);
                return result != null;
            },
            TimeSpan.FromMilliseconds(timeoutMs ?? DefaultTimeoutMs));

        return result;
    }

    private static void SetElementText(IMauiElement element, string text)
    {
        if (element is Interfaces.INestedTextElement textElement)
        {
            textElement.ClearWithFallback();
            if (textElement.SetTextWithFallback(text))
            {
                return;
            }
        }
        else
        {
            element.Clear();
        }

        element.SendKeys(text, TextInputMethod.SetValue);
    }
}
