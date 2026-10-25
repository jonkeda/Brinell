using Bruinell.Presenter.Models;

namespace Bruinell.Presenter.Services;

public interface IBddWorkspaceService
{
    string? FindDefaultWorkspace();

    BddWorkspaceLoadResult LoadFolder(string folderPath);
}
