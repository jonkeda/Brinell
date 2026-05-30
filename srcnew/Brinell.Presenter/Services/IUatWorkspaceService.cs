using Brinell.Presenter.Models;

namespace Brinell.Presenter.Services;

public interface IUatWorkspaceService
{
    string? FindDefaultWorkspace();

    UatWorkspaceLoadResult LoadFolder(string folderPath);
}
