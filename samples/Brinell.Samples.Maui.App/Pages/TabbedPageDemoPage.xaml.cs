namespace Brinell.Samples.Maui.App.Pages;

/// <summary>
/// Demonstrates TabbedPage with AutomationId support for UI testing.
/// 
/// Each child ContentPage has an AutomationId that is mapped to the 
/// corresponding tab element on Windows via TabbedPageAutomationMapper.
/// </summary>
public partial class TabbedPageDemoPage : TabbedPage
{
    public TabbedPageDemoPage()
    {
        InitializeComponent();
    }
}
