using System;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Windows.ApplicationModel.DataTransfer;

namespace DelphiModernizer.Studio.App.Services;

public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }
}
