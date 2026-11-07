using System;
using System.Threading.Tasks;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DelphiModernizer.Studio.App.Services;

/// <summary>The system pickers, owned by the main window as an unpackaged app must set them.</summary>
public sealed class PickerService : IPickerService
{
    private readonly MainWindow _window;

    public PickerService(MainWindow window)
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

    public async Task<string?> PickFileAsync(string extension)
    {
        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.ComputerFolder };
        picker.FileTypeFilter.Add(extension);
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(_window));
        var file = await picker.PickSingleFileAsync();

        return file?.Path;
    }
}
