namespace Brinell.Presenter.Services;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);

    /// <summary>Asks for a project file.</summary>
    /// <param name="cancellationToken">Cancels the prompt.</param>
    /// <returns>The chosen <c>.csproj</c>, or null when cancelled.</returns>
    Task<string?> PickProjectAsync(CancellationToken cancellationToken = default);
}
