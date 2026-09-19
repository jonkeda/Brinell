namespace ClaudeSwitcher.Mvvm;

public interface IViewVisible
{
    bool ViewVisible { get; }
    bool IsBusy { get; }
    void BeginBusy();
    void EndBusy();
}
