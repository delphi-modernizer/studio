using System.Threading.Tasks;

namespace DelphiModernizer.Studio.UI.Contracts.Services;

public interface IFolderPickerService
{
    /// <returns>The chosen folder, or null when the person cancelled.</returns>
    Task<string?> PickFolderAsync();
}
