using Brinell.Scraper.ViewModels;

namespace Brinell.Scraper.Models;

public sealed class ControlPropertyItem : ViewModelBase
{
    private string _name = "";
    private string _controlType = "";
    private string _selector = "";

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string ControlType
    {
        get => _controlType;
        set => SetProperty(ref _controlType, value);
    }

    public string Selector
    {
        get => _selector;
        set => SetProperty(ref _selector, value);
    }
}
