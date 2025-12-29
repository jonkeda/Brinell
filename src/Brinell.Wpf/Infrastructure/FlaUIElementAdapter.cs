using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;

namespace Brinell.Wpf.Infrastructure;

/// <summary>
/// FlaUI element adapter implementing IElementAdapter.
/// </summary>
public class FlaUIElementAdapter : IElementAdapter
{
    private readonly AutomationElement _element;
    
    public FlaUIElementAdapter(AutomationElement element)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
    }
    
    public string AutomationId => _element.AutomationId ?? string.Empty;
    
    public object NativeElement => _element;
    
    /// <summary>
    /// Get the underlying FlaUI AutomationElement.
    /// </summary>
    public AutomationElement Element => _element;
}
