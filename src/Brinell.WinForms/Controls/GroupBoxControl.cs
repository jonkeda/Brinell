using System.Collections.Generic;
using System.Linq;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms GroupBox control wrapper.
/// Provides container access and label operations.
/// </summary>
public class GroupBoxControl : ControlBase
{
    public GroupBoxControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public GroupBoxControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public GroupBoxControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the GroupBox label/title.
    /// </summary>
    public string GetLabel()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetLabel", $"Element '{AutomationId}' not found.");
        }

        var label = element!.Name ?? string.Empty;
        LogAction("GetLabel", label);
        return label;
    }

    /// <summary>
    /// Assert that the label matches expected.
    /// </summary>
    public void AssertLabelEquals(string expected)
    {
        var actual = GetLabel();
        if (actual != expected)
        {
            ThrowAssertionFailed("LabelEquals", actual, expected,
                $"GroupBox '{AutomationId}' label is '{actual}', expected '{expected}'.");
        }
        LogAssertPass("LabelEquals", actual, expected);
    }

    /// <summary>
    /// Assert that the label contains expected text.
    /// </summary>
    public void AssertLabelContains(string expectedText)
    {
        var actual = GetLabel();
        if (!actual.Contains(expectedText, System.StringComparison.Ordinal))
        {
            ThrowAssertionFailed("LabelContains", actual, expectedText,
                $"GroupBox '{AutomationId}' label '{actual}' does not contain '{expectedText}'.");
        }
        LogAssertPass("LabelContains", actual, expectedText);
    }

    /// <summary>
    /// Get the number of child controls.
    /// </summary>
    public int GetChildCount()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetChildCount", $"Element '{AutomationId}' not found.");
        }

        try
        {
            var children = element!.FindAllChildren().ToList();
            LogAction("GetChildCount", children.Count.ToString());
            return children.Count;
        }
        catch (Exception ex)
        {
            ThrowCheckFailed("GetChildCount", $"Failed to get child count: {ex.Message}");
        }

        return 0;
    }

    /// <summary>
    /// Get names of all child controls.
    /// </summary>
    public List<string> GetChildNames()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetChildNames", $"Element '{AutomationId}' not found.");
        }

        var names = new List<string>();
        try
        {
            var children = element!.FindAllChildren();
            foreach (var child in children)
            {
                if (!string.IsNullOrEmpty(child.AutomationId))
                {
                    names.Add(child.AutomationId);
                }
                else if (!string.IsNullOrEmpty(child.Name))
                {
                    names.Add(child.Name);
                }
            }
            LogAction("GetChildNames", $"{names.Count} children");
        }
        catch (Exception ex)
        {
            ThrowCheckFailed("GetChildNames", $"Failed to get child names: {ex.Message}");
        }

        return names;
    }

    /// <summary>
    /// Check if a child control exists by AutomationId.
    /// </summary>
    public bool ChildExists(string automationId)
    {
        var element = FindElement();
        if (element == null)
        {
            return false;
        }

        try
        {
            var child = element!.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
            var exists = child != null;
            LogAction("ChildExists", $"{automationId}: {exists}");
            return exists;
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to check if child exists: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Assert that the child control exists.
    /// </summary>
    public void AssertChildExists(string automationId)
    {
        if (!ChildExists(automationId))
        {
            ThrowAssertionFailed("ChildExists", "not found", "exists",
                $"GroupBox '{AutomationId}' child '{automationId}' does not exist.");
        }
        LogAssertPass("ChildExists", automationId, "exists");
    }

    /// <summary>
    /// Assert that the child count matches expected.
    /// </summary>
    public void AssertChildCount(int expected)
    {
        var actual = GetChildCount();
        if (actual != expected)
        {
            ThrowAssertionFailed("ChildCount", actual.ToString(), expected.ToString(),
                $"GroupBox '{AutomationId}' has {actual} children, expected {expected}.");
        }
        LogAssertPass("ChildCount", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Check if the GroupBox is visible and enabled.
    /// </summary>
    public override bool IsEnabled()
    {
        var element = FindElement();
        if (element == null) return false;
        return element.IsEnabled && !element.IsOffscreen;
    }

    /// <summary>
    /// Assert that the GroupBox is enabled and visible.
    /// </summary>
    public void AssertIsEnabled()
    {
        if (!IsEnabled())
        {
            ThrowAssertionFailed("IsEnabled", "false", "true",
                $"GroupBox '{AutomationId}' is not enabled or visible.");
        }
        LogAssertPass("IsEnabled", "true", "true");
    }
}
