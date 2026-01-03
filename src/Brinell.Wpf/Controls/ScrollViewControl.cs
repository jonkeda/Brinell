using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Patterns;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF ScrollViewer control wrapper.
/// Implements IScrollableControl for scrolling support (FR-002.7).
/// </summary>
public class ScrollViewControl : ControlBase, IScrollableControl
{
    /// <summary>
    /// Maximum number of scroll attempts when searching for an element.
    /// </summary>
    public int MaxScrollAttempts { get; set; } = 20;
    
    /// <summary>
    /// Default scroll distance in percentage (0-100).
    /// </summary>
    public double DefaultScrollPercent { get; set; } = 20;

    public ScrollViewControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ScrollViewControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public ScrollViewControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the scroll pattern from the element.
    /// </summary>
    private IScrollPattern? GetScrollPattern()
    {
        var element = FindElement();
        if (element == null) return null;
        
        return element.Patterns.Scroll.PatternOrDefault;
    }

    /// <summary>
    /// Check if element with the given automation ID is visible within this scroll view.
    /// </summary>
    private bool IsElementVisible(string automationId)
    {
        var element = FindElement();
        if (element == null) return false;
        
        var target = element.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        return target != null && !target.IsOffscreen;
    }

    /// <summary>
    /// Scroll until the element with the specified automation ID is visible.
    /// </summary>
    public void ScrollToElement(string automationId)
    {
        LogAction("ScrollToElement", automationId);
        
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("ScrollToElement", $"ScrollViewer '{AutomationId}' not found.");
            return;
        }
        
        // First check if already visible
        if (IsElementVisible(automationId))
        {
            LogDebug($"Element '{automationId}' already visible.");
            return;
        }
        
        // Scroll to top first
        ScrollToTop();
        
        // Check again after scrolling to top
        if (IsElementVisible(automationId))
        {
            LogDebug($"Element '{automationId}' found at top.");
            return;
        }
        
        // Scroll down until found or max attempts
        for (int i = 0; i < MaxScrollAttempts; i++)
        {
            ScrollDown((int)DefaultScrollPercent);
            
            if (IsElementVisible(automationId))
            {
                LogDebug($"Element '{automationId}' found after {i + 1} scrolls.");
                return;
            }
            
            // Check if we've reached the bottom
            var scroll = GetScrollPattern();
            if (scroll != null && scroll.VerticalScrollPercent >= 100)
            {
                break;
            }
        }
        
        ThrowCheckFailed("ScrollToElement", 
            $"Element '{automationId}' not found in ScrollViewer '{AutomationId}' after {MaxScrollAttempts} scroll attempts.");
    }

    /// <summary>
    /// Scroll to the top of the content.
    /// </summary>
    public void ScrollToTop()
    {
        LogAction("ScrollToTop");
        
        var scroll = GetScrollPattern();
        if (scroll == null)
        {
            LogDebug("No scroll pattern available - element may not be scrollable.");
            return;
        }
        
        // Set vertical scroll to 0%
        if (scroll.VerticallyScrollable)
        {
            scroll.SetScrollPercent(scroll.HorizontalScrollPercent, 0);
        }
    }

    /// <summary>
    /// Scroll to the bottom of the content.
    /// </summary>
    public void ScrollToBottom()
    {
        LogAction("ScrollToBottom");
        
        var scroll = GetScrollPattern();
        if (scroll == null)
        {
            LogDebug("No scroll pattern available - element may not be scrollable.");
            return;
        }
        
        // Set vertical scroll to 100%
        if (scroll.VerticallyScrollable)
        {
            scroll.SetScrollPercent(scroll.HorizontalScrollPercent, 100);
        }
    }

    /// <summary>
    /// Scroll up by the specified distance (percentage).
    /// </summary>
    public void ScrollUp(int distance = 100)
    {
        LogAction("ScrollUp", distance.ToString());
        
        var scroll = GetScrollPattern();
        if (scroll == null || !scroll.VerticallyScrollable)
        {
            LogDebug("Cannot scroll up - not vertically scrollable.");
            return;
        }
        
        // Calculate new position (distance is treated as percentage for consistency)
        var percent = distance > 100 ? DefaultScrollPercent : distance;
        var newPercent = Math.Max(0, scroll.VerticalScrollPercent - percent);
        scroll.SetScrollPercent(scroll.HorizontalScrollPercent, newPercent);
    }

    /// <summary>
    /// Scroll down by the specified distance (percentage).
    /// </summary>
    public void ScrollDown(int distance = 100)
    {
        LogAction("ScrollDown", distance.ToString());
        
        var scroll = GetScrollPattern();
        if (scroll == null || !scroll.VerticallyScrollable)
        {
            LogDebug("Cannot scroll down - not vertically scrollable.");
            return;
        }
        
        // Calculate new position (distance is treated as percentage for consistency)
        var percent = distance > 100 ? DefaultScrollPercent : distance;
        var newPercent = Math.Min(100, scroll.VerticalScrollPercent + percent);
        scroll.SetScrollPercent(scroll.HorizontalScrollPercent, newPercent);
    }

    /// <summary>
    /// Scroll left by the specified distance (percentage).
    /// </summary>
    public void ScrollLeft(int distance = 100)
    {
        LogAction("ScrollLeft", distance.ToString());
        
        var scroll = GetScrollPattern();
        if (scroll == null || !scroll.HorizontallyScrollable)
        {
            LogDebug("Cannot scroll left - not horizontally scrollable.");
            return;
        }
        
        var percent = distance > 100 ? DefaultScrollPercent : distance;
        var newPercent = Math.Max(0, scroll.HorizontalScrollPercent - percent);
        scroll.SetScrollPercent(newPercent, scroll.VerticalScrollPercent);
    }

    /// <summary>
    /// Scroll right by the specified distance (percentage).
    /// </summary>
    public void ScrollRight(int distance = 100)
    {
        LogAction("ScrollRight", distance.ToString());
        
        var scroll = GetScrollPattern();
        if (scroll == null || !scroll.HorizontallyScrollable)
        {
            LogDebug("Cannot scroll right - not horizontally scrollable.");
            return;
        }
        
        var percent = distance > 100 ? DefaultScrollPercent : distance;
        var newPercent = Math.Min(100, scroll.HorizontalScrollPercent + percent);
        scroll.SetScrollPercent(newPercent, scroll.VerticalScrollPercent);
    }

    #region Scroll State Properties

    /// <summary>
    /// Get the current vertical scroll percentage (0-100).
    /// </summary>
    public double GetVerticalScrollPercent()
    {
        var scroll = GetScrollPattern();
        return scroll?.VerticalScrollPercent.Value ?? 0;
    }

    /// <summary>
    /// Get the current horizontal scroll percentage (0-100).
    /// </summary>
    public double GetHorizontalScrollPercent()
    {
        var scroll = GetScrollPattern();
        return scroll?.HorizontalScrollPercent.Value ?? 0;
    }

    /// <summary>
    /// Check if the scroll view is vertically scrollable.
    /// </summary>
    public bool IsVerticallyScrollable()
    {
        var scroll = GetScrollPattern();
        return scroll?.VerticallyScrollable.Value ?? false;
    }

    /// <summary>
    /// Check if the scroll view is horizontally scrollable.
    /// </summary>
    public bool IsHorizontallyScrollable()
    {
        var scroll = GetScrollPattern();
        return scroll?.HorizontallyScrollable.Value ?? false;
    }

    #endregion
}
