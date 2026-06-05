namespace Brinell.NativeAndroid.Controls;

public abstract class AndroidSelectorControlBase<TScope> : NativeAndroidControl<TScope>, ISelectorControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    protected AndroidSelectorControlBase(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    protected AndroidSelectorControlBase(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public virtual TScope SelectByText(string? text, int? timeoutMs = null)
    {
        if (text is null)
        {
            return ContainingScope;
        }

        var root = FindElementForAction(timeoutMs);
        if (TryFindItemByText(root, text, timeoutMs, out var item)
            || TryOpenAndFindItemByText(root, text, timeoutMs, out item))
        {
            item!.Click();
            return ContainingScope;
        }

        throw new ElementNotFoundException(NativeAndroidLocator.ByTextOrDescription(text), timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope SelectByIndex(int? index, int? timeoutMs = null)
    {
        if (index is null)
        {
            return ContainingScope;
        }

        if (index.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be zero or greater.");
        }

        var root = FindElementForAction(timeoutMs);
        var items = GetItemElementsCore(root);
        if (index.Value < items.Count)
        {
            items[index.Value].Click();
            return ContainingScope;
        }

        root.Click();
        items = Context.Driver.FindElements(Locator.ByXPath("//*[string-length(@text) > 0 or string-length(@content-desc) > 0]"), timeoutMs ?? DefaultTimeoutMs);
        if (index.Value < items.Count)
        {
            items[index.Value].Click();
            return ContainingScope;
        }

        throw new ArgumentOutOfRangeException(nameof(index), $"Only {items.Count} selectable items were found.");
    }

    public virtual TScope SelectByValue(string? value, int? timeoutMs = null)
    {
        if (value is null)
        {
            return ContainingScope;
        }

        var literal = NativeAndroidByExtensions.ToXPathLiteral(value);
        var locator = Locator.ByXPath(
            $"//*[@text={literal} or @content-desc={literal} or @resource-id={literal}]");

        var root = FindElementForAction(timeoutMs);
        root.Click();
        Context.Driver.FindElement(locator, timeoutMs ?? DefaultTimeoutMs).Click();
        return ContainingScope;
    }

    public virtual string? GetSelectedText(int? timeoutMs = null)
    {
        var element = GetElementForRead(timeoutMs);
        if (element is null)
        {
            return null;
        }

        var selected = GetItemElementsCore(element).FirstOrDefault(IsSelectedItem);
        return TextOrDescription(selected ?? element);
    }

    public virtual int? GetSelectedIndex(int? timeoutMs = null)
    {
        var element = GetElementForRead(timeoutMs);
        if (element is null)
        {
            return null;
        }

        var items = GetItemElementsCore(element);
        for (var index = 0; index < items.Count; index++)
        {
            if (IsSelectedItem(items[index]))
            {
                return index;
            }
        }

        return null;
    }

    public virtual IReadOnlyList<string>? GetItemTexts(int? timeoutMs = null)
    {
        var element = GetElementForRead(timeoutMs);
        if (element is null)
        {
            return null;
        }

        return GetItemElementsCore(element)
            .Select(TextOrDescription)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text!)
            .ToList();
    }

    public virtual int? GetItemCount(int? timeoutMs = null)
        => GetItemTexts(timeoutMs)?.Count;

    public virtual bool WaitSelectedText(string? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => string.Equals(GetSelectedText(), expected, StringComparison.Ordinal), timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitSelectedText(expected, timeoutMs))
        {
            Fail(message ?? $"Expected selected text '{expected}', actual '{GetSelectedText()}'.", expected, GetSelectedText());
        }

        return ContainingScope;
    }

    public virtual bool WaitSelectedIndex(int? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => GetSelectedIndex() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitSelectedIndex(expected, timeoutMs))
        {
            Fail(message ?? $"Expected selected index '{expected}', actual '{GetSelectedIndex()}'.", expected, GetSelectedIndex());
        }

        return ContainingScope;
    }

    public virtual bool WaitItemCount(int? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => GetItemCount() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public virtual TScope AssertItemCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitItemCount(expected, timeoutMs))
        {
            Fail(message ?? $"Expected item count '{expected}', actual '{GetItemCount()}'.", expected, GetItemCount());
        }

        return ContainingScope;
    }

    protected virtual IReadOnlyList<NativeAndroidElement> GetItemElementsCore(NativeAndroidElement root)
        => root.FindElements(Locator.ByXPath(
            ".//*[self::android.widget.TextView or self::android.widget.CheckedTextView or self::android.widget.RadioButton or self::android.widget.CheckBox or self::android.view.ViewGroup or string-length(@text) > 0 or string-length(@content-desc) > 0]"));

    protected virtual bool IsSelectedItem(NativeAndroidElement element)
    {
        foreach (var attribute in new[] { "selected", "checked", "isChecked", "value" })
        {
            var parsed = AndroidToggleControlBase<TScope>.TryParseBoolean(element.GetAttribute(attribute));
            if (parsed is not null)
            {
                return parsed.Value;
            }
        }

        return element.Selected;
    }

    protected static string? TextOrDescription(NativeAndroidElement? element)
        => string.IsNullOrWhiteSpace(element?.Text)
            ? element?.ContentDescription
            : element.Text;

    private bool TryOpenAndFindItemByText(
        NativeAndroidElement root,
        string text,
        int? timeoutMs,
        out NativeAndroidElement? item)
    {
        root.Click();
        return Context.Driver.TryFindElement(NativeAndroidLocator.ByTextOrDescription(text), out item, timeoutMs ?? DefaultTimeoutMs)
            || Context.Driver.TryFindElement(NativeAndroidLocator.ByTextContains(text), out item, timeoutMs ?? DefaultTimeoutMs);
    }

    private static bool TryFindItemByText(
        NativeAndroidElement root,
        string text,
        int? timeoutMs,
        out NativeAndroidElement? item)
    {
        try
        {
            item = root.FindElement(NativeAndroidLocator.ByTextOrDescription(text), timeoutMs ?? 0);
            return true;
        }
        catch (ElementNotFoundException)
        {
            try
            {
                item = root.FindElement(NativeAndroidLocator.ByTextContains(text), timeoutMs ?? 0);
                return true;
            }
            catch (ElementNotFoundException)
            {
                item = null;
                return false;
            }
        }
    }

    private NativeAndroidElement? GetElementForRead(int? timeoutMs)
    {
        if (timeoutMs is null)
        {
            return TryFindElement();
        }

        try
        {
            return FindElement(timeoutMs.Value);
        }
        catch (ElementNotFoundException)
        {
            return null;
        }
    }
}
