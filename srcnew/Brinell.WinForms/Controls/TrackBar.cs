namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms TrackBar control (slider).
/// </summary>
public sealed class TrackBar<TScope> : RangeControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public TrackBar(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TrackBar(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
