namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// The greeting demo: a name in, a greeting or a validation message out.
/// </summary>
/// <remarks>
/// Both outcomes go to the same label, because the UAT scenarios assert on one control either
/// way: "Hello, Alice!" when a name is entered, "Please enter your name" when it is empty.
/// </remarks>
public class GreetingViewModel : ParentViewModel
{
    private string name = string.Empty;
    private string greeting = string.Empty;

    /// <summary>The name to greet.</summary>
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    /// <summary>The greeting, or the validation message when no name was entered.</summary>
    public string Greeting
    {
        get => greeting;
        set => SetProperty(ref greeting, value);
    }

    /// <summary>Produces the greeting.</summary>
    public ICommand GreetCommand => new RelayCommand(Greet);

    private void Greet()
    {
        Greeting = string.IsNullOrWhiteSpace(Name)
            ? "Please enter your name"
            : $"Hello, {Name}!";
    }
}
