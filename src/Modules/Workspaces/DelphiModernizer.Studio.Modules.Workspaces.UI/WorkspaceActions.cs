using System;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Commands;
using DelphiModernizer.Studio.UI.Contracts.Services;

namespace DelphiModernizer.Studio.Modules.Workspaces.UI;

/// <summary>New workspace and Open folder, as every page offers them while no workspace is open.</summary>
internal sealed class WorkspaceActions : IWorkspaceActions
{
    private readonly ISender _sender;
    private readonly IPickerService _picker;
    private readonly INavigationService _navigation;

    public WorkspaceActions(ISender sender, IPickerService picker, INavigationService navigation)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(picker);
        ArgumentNullException.ThrowIfNull(navigation);
        _sender = sender;
        _picker = picker;
        _navigation = navigation;
    }

    public void StartNewWorkspace()
    {
        _navigation.NavigateTo("NewWorkspace");
    }

    /// <remarks>Opening a workspace forgets the pages before it and lands on Overview (navigation model).</remarks>
    public async Task<string?> OpenFolderAsync()
    {
        if (await _picker.PickFolderAsync() is not { } folder)
        {
            return null;
        }

        var opened = await _sender.Send(new OpenWorkspaceCommand(folder));

        if (opened.IsFailure)
        {
            return opened.Error.Message;
        }

        _navigation.NavigateFresh("Overview");

        return null;
    }
}
