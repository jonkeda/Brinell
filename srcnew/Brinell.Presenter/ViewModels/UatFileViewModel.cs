using Brinell.Presenter.Models;

namespace Brinell.Presenter.ViewModels;

public sealed class UatFileViewModel : ViewModelBase
{
    public UatFileViewModel(UatFileLoadResult source)
    {
        Name = source.Name;
        FilePath = source.FilePath;
        Status = source.ParseSucceeded && source.BindSucceeded ? "ok" : "error";
        Diagnostics = string.Join(Environment.NewLine, source.Diagnostics);
    }

    public string Name { get; }

    public string FilePath { get; }

    public string Status { get; }

    public string Diagnostics { get; }
}
