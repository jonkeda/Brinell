using Brinell.Presenter.Models;

namespace Brinell.Presenter.Services;

public interface IBddWorkspaceService
{
    string? FindDefaultWorkspace();

    BddWorkspaceLoadResult LoadFolder(string folderPath);
}
