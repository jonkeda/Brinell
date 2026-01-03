using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Controls.Base;

/// <summary>
/// WPF base class for controls with numeric range values (slider, progress bar).
/// </summary>
public abstract class RangeControlBase : ControlBase, IRangeControl
{
    protected RangeControlBase(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a control that searches within a container element.
    /// </summary>
    protected RangeControlBase(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected RangeControlBase(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the Slider pattern from the element.
    /// </summary>
    protected Slider? GetSlider()
    {
        var element = FindElement();
        return element?.AsSlider();
    }

    /// <summary>
    /// Get the ProgressBar pattern from the element.
    /// </summary>
    protected ProgressBar? GetProgressBar()
    {
        var element = FindElement();
        return element?.AsProgressBar();
    }

    /// <summary>
    /// Get the current value.
    /// </summary>
    public virtual double GetValue()
    {
        var slider = GetSlider();
        if (slider != null)
        {
            return slider.Value;
        }
        
        var progressBar = GetProgressBar();
        if (progressBar != null)
        {
            return progressBar.Value;
        }
        
        return 0;
    }

    /// <summary>
    /// Get the minimum value.
    /// </summary>
    public virtual double GetMinimum()
    {
        var slider = GetSlider();
        if (slider != null)
        {
            return slider.Minimum;
        }
        
        var progressBar = GetProgressBar();
        if (progressBar != null)
        {
            return progressBar.Minimum;
        }
        
        return 0;
    }

    /// <summary>
    /// Get the maximum value.
    /// </summary>
    public virtual double GetMaximum()
    {
        var slider = GetSlider();
        if (slider != null)
        {
            return slider.Maximum;
        }
        
        var progressBar = GetProgressBar();
        if (progressBar != null)
        {
            return progressBar.Maximum;
        }
        
        return 100;
    }

    /// <summary>
    /// Set the value.
    /// </summary>
    public virtual void SetValue(double value)
    {
        LogAction("SetValue", value.ToString());
        var slider = GetSlider();
        if (slider != null)
        {
            slider.Value = value;
        }
    }

    /// <summary>
    /// Increment the value.
    /// </summary>
    public virtual void Increment()
    {
        LogAction("Increment");
        var slider = GetSlider();
        slider?.SmallIncrement();
    }

    /// <summary>
    /// Decrement the value.
    /// </summary>
    public virtual void Decrement()
    {
        LogAction("Decrement");
        var slider = GetSlider();
        slider?.SmallDecrement();
    }

    /// <summary>
    /// Assert value equals expected (within tolerance).
    /// </summary>
    public virtual void AssertValue(double expected, double tolerance = 0.001, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetValue();
        if (Math.Abs(actual - expected) > tolerance)
        {
            ThrowAssertionFailed("Value", actual.ToString(), expected.ToString(),
                message ?? $"Expected value '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("Value", actual.ToString(), expected.ToString());
    }
}
