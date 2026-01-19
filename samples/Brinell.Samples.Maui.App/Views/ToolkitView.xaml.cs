using Brinell.Samples.Maui.App.ViewModels;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace Brinell.Samples.Maui.App.Views;

public partial class ToolkitView : ContentView
{
    private Button? _selectedSegment;
    
    public ToolkitView()
    {
        InitializeComponent();
    }

    private async void OnShowPopupClicked(object? sender, EventArgs e)
    {
        var popup = new SamplePopup();
        
        // Get the parent page to show the popup
        var page = this.GetParentPage();
        if (page == null) return;
        
        var result = await page.ShowPopupAsync(popup);
        
        if (BindingContext is ToolkitViewModel viewModel)
        {
            viewModel.SetPopupResult(result?.ToString() ?? "Cancelled");
        }
    }

    private async void OnShowSnackbarClicked(object? sender, EventArgs e)
    {
        var snackbarOptions = new SnackbarOptions
        {
            BackgroundColor = Colors.DarkSlateGray,
            TextColor = Colors.White,
            ActionButtonTextColor = Colors.Yellow,
            CornerRadius = new CornerRadius(10),
            Font = Microsoft.Maui.Font.SystemFontOfSize(14),
        };

        var snackbar = Snackbar.Make(
            "This is a snackbar message!",
            async () => 
            {
                if (BindingContext is ToolkitViewModel viewModel)
                {
                    viewModel.SnackbarMessage = "Snackbar action clicked!";
                }
                await Task.CompletedTask;
            },
            "Dismiss",
            TimeSpan.FromSeconds(3),
            snackbarOptions);

        await snackbar.Show();
        
        if (BindingContext is ToolkitViewModel vm)
        {
            vm.SnackbarMessage = "Snackbar shown!";
        }
    }
    
    private void OnSegmentClicked(object? sender, EventArgs e)
    {
        if (sender is not Button clickedButton) return;
        
        // Reset previous selection
        if (_selectedSegment != null)
        {
            _selectedSegment.BackgroundColor = Colors.LightGray;
            _selectedSegment.TextColor = Colors.Black;
        }
        
        // Set new selection
        clickedButton.BackgroundColor = (Color)Application.Current!.Resources["Primary"];
        clickedButton.TextColor = Colors.White;
        _selectedSegment = clickedButton;
        
        // Update content based on selection
        var automationId = clickedButton.AutomationId;
        switch (automationId)
        {
            case "SegmentInfo":
                SegmentTitle.Text = "Information";
                SegmentDescription.Text = "This demonstrates a custom segmented control. Click different segments to switch content.";
                break;
            case "SegmentSettings":
                SegmentTitle.Text = "Settings";
                SegmentDescription.Text = "Configure your preferences here. This panel would contain switches, sliders, and other settings controls.";
                break;
            case "SegmentAbout":
                SegmentTitle.Text = "About";
                SegmentDescription.Text = "Brinell MAUI Sample v1.0\nPowered by CommunityToolkit.Maui";
                break;
        }
    }
    
    private Page? GetParentPage()
    {
        Element? parent = this.Parent;
        while (parent != null)
        {
            if (parent is Page page)
                return page;
            parent = parent.Parent;
        }
        return null;
    }
}
