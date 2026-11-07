using System.Threading.Tasks;

namespace DelphiModernizer.Studio.UI.Contracts.Services;

/// <summary>Opens a workspace file in the person's editor, at a line when one is given, or a folder in Explorer.</summary>
public interface IEditorLauncher
{
    /// <param name="relativePath">A path inside the open workspace, such as <c>legacy/Sales/Pricing.pas</c>.</param>
    /// <returns>Whether it opened; false when no workspace is open or the path does not exist.</returns>
    Task<bool> OpenAsync(string relativePath, int? line);
}
