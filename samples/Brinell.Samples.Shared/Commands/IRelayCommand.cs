using System.Windows.Input;

namespace Brinell.Samples.Shared.Commands;

/// <summary>
/// An interface expanding <see cref="ICommand"/> with the ability to raise
/// the <see cref="ICommand.CanExecuteChanged"/> event externally.
/// </summary>
public interface IRelayCommand : ICommand
{
    /// <summary>
    /// Notifies that the <see cref="ICommand.CanExecuteChanged"/> event has been raised.
    /// </summary>
    void NotifyCanExecuteChanged();
}
