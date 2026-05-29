namespace Brinell.Maui.Controls;

internal static class ElementSearch
{
    public static bool HasUsableBounds(IMauiElement? element)
    {
        try
        {
            return element?.Visible == true
                && element.Rect is { Width: > 0, Height: > 0 };
        }
        catch
        {
            return false;
        }
    }

    public static IMauiElement? FirstVisible(IEnumerable<IMauiElement>? elements)
        => elements?.FirstOrDefault(HasUsableBounds);

    public static IMauiElement? FindVisibleByAutomationId(IMauiElementScope scope, string automationId)
        => FirstVisible(scope.FindElements(Locator.ByAutomationId(automationId)));

    public static IMauiElement? FindVisibleByName(IMauiElementScope scope, string name)
        => FirstVisible(scope.FindElements(Locator.ByName(name)));

    public static bool IsControlType(IMauiElement element, string controlType)
    {
        try
        {
            return string.Equals(element.TagName, controlType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(element.GetAttribute("controltype"), controlType, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static IMauiElement? FindChildByAutomationId(
        IMauiElementScope scope,
        IMauiElement root,
        string automationId)
    {
        var directChild = FirstVisible(root.FindElements(Locator.ByAutomationId(automationId)));
        if (directChild != null)
        {
            return directChild;
        }

        if (!HasUsableBounds(root))
        {
            return null;
        }

        return (scope.FindElements(Locator.ByAutomationId(automationId)) ?? Array.Empty<IMauiElement>())
            .Where(HasUsableBounds)
            .FirstOrDefault(candidate => ContainsCenter(root, candidate));
    }

    public static IMauiElement? FindChildByControlType(
        IMauiElementScope scope,
        IMauiElement root,
        string controlType)
    {
        var directChild = FirstVisible(root.FindElements(Locator.ByControlType(controlType)));
        if (directChild != null)
        {
            return directChild;
        }

        if (!HasUsableBounds(root))
        {
            return null;
        }

        return (scope.FindElements(Locator.ByControlType(controlType)) ?? Array.Empty<IMauiElement>())
            .Where(HasUsableBounds)
            .Where(candidate => ContainsCenter(root, candidate))
            .OrderBy(candidate => candidate.Rect.Width * candidate.Rect.Height)
            .FirstOrDefault();
    }

    public static IReadOnlyList<IMauiElement> FindContainingListItems(
        IMauiElementScope scope,
        IMauiElement element)
    {
        if (!HasUsableBounds(element))
        {
            return Array.Empty<IMauiElement>();
        }

        var center = CenterOf(element.Rect);
        return (scope.FindElements(Locator.ByControlType("ListItem")) ?? Array.Empty<IMauiElement>())
            .Where(HasUsableBounds)
            .Where(item => item.Rect.Contains(center))
            .OrderBy(item => item.Rect.Width * item.Rect.Height)
            .ToList();
    }

    public static bool WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        return SpinWait.SpinUntil(
            () =>
            {
                try
                {
                    return condition();
                }
                catch
                {
                    return false;
                }
            },
            timeout);
    }

    public static bool ContainsCenter(IMauiElement parent, IMauiElement child)
    {
        var center = CenterOf(child.Rect);
        return parent.Rect.Contains(center);
    }

    private static System.Drawing.Point CenterOf(System.Drawing.Rectangle rectangle)
    {
        return new System.Drawing.Point(
            rectangle.X + rectangle.Width / 2,
            rectangle.Y + rectangle.Height / 2);
    }
}
