using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;

using ProgressBar = FlaUI.Core.AutomationElements.ProgressBar;

namespace Brinell.FlaUI.Controls.Base;

/// <summary>
/// Shared base class for controls with numeric range values (slider, progress bar).
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
    /// Get the Spinner pattern from the element (for NumericUpDown).
    /// </summary>
    protected Spinner? GetSpinner()
    {
        var element = FindElement();
        return element?.AsSpinner();
    }

    /// <summary>
    /// Get the current value.
    /// </summary>
    public virtual double GetValue()
    {
        var element = GetRequiredElement("GetValue");
        
        var slider = element.AsSlider();
        if (slider != null)
        {
            return slider.Value;
        }
        
        var progressBar = element.AsProgressBar();
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
        var element = GetRequiredElement("GetMinimum");
        
        var slider = element.AsSlider();
        if (slider != null)
        {
            return slider.Minimum;
        }
        
        var progressBar = element.AsProgressBar();
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
        var element = GetRequiredElement("GetMaximum");
        
        var slider = element.AsSlider();
        if (slider != null)
        {
            return slider.Maximum;
        }
        
        var progressBar = element.AsProgressBar();
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
