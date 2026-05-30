namespace Brinell.Presenter.Services;

public sealed class FolderPickerService : IFolderPickerService
{
    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

#if WINDOWS
        var picker = new Windows.Storage.Pickers.FolderPicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add("*");

        var window = Application.Current?.Windows.FirstOrDefault();
        var platformWindow = window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (platformWindow is not null)
        {
            var handle = WinRT.Interop.WindowNative.GetWindowHandle(platformWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, handle);
        }

        var folder = await picker.PickSingleFolderAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return folder?.Path;
#else
        await Task.CompletedTask;
        return null;
#endif
    }
}
