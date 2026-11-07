using System.Threading.Tasks;

namespace DelphiModernizer.Studio.UI.Contracts.Services;

/// <summary>The system pickers; each returns the chosen path, or null when the person cancelled.</summary>
public interface IPickerService
{
    Task<string?> PickFolderAsync();

    /// <param name="extension">The one file type offered, with its dot, such as <c>.exe</c>.</param>
    Task<string?> PickFileAsync(string extension);
}
