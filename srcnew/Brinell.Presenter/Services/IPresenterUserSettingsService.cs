namespace Brinell.Presenter.Services;

public interface IPresenterUserSettingsService
{
    PresenterUserSettings Load();

    void Save(PresenterUserSettings settings);

    PresenterUserSettings RecordOpenedFolder(string folderPath);
}
