using Bruinet.Presenter.Models;

namespace Bruinet.Presenter.ViewModels;

public sealed class BddFileViewModel : ViewModelBase
{
    public BddFileViewModel(BddFileLoadResult source)
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
