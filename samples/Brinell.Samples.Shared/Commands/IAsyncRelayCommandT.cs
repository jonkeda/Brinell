namespace Brinell.Samples.Shared.Commands;

/// <summary>
/// A generic interface representing a more specific version of <see cref="IAsyncRelayCommand"/>.
/// </summary>
/// <typeparam name="T">The type of parameter being passed as input to the callbacks.</typeparam>
public interface IAsyncRelayCommand<in T> : IAsyncRelayCommand, IRelayCommand<T>
{
    /// <summary>
    /// Provides a strongly-typed variant of <see cref="IAsyncRelayCommand.ExecuteAsync(object)"/>,
    /// also returning the <see cref="Task"/> representing the async operation being executed.
    /// </summary>
    /// <param name="parameter">The input parameter.</param>
    /// <returns>The <see cref="Task"/> representing the async operation being executed.</returns>
    Task ExecuteAsync(T? parameter);
}
