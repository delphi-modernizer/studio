namespace DelphiModernizer.Studio.UI.Contracts.Services;

/// <summary>Puts text on the Windows clipboard, for Copy versions and Copy the fingerprint.</summary>
public interface IClipboardService
{
    void SetText(string text);
}
