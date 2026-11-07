using System;
using System.Threading.Tasks;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DelphiModernizer.Studio.App.Services;

/// <summary>The system folder picker, owned by the main window as an unpackaged app must set it.</summary>
public sealed class FolderPickerService : IFolderPickerService
{
    private readonly MainWindow _window;

    public FolderPickerService(MainWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        _window = window;
    }

    public async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(_window));
        var folder = await picker.PickSingleFolderAsync();

        return folder?.Path;
    }
}
