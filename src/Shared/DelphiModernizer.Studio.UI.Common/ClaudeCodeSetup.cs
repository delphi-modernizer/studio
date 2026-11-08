using System;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Agents;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Settings;
using DelphiModernizer.Studio.UI.Common.Controls;
using DelphiModernizer.Studio.UI.Contracts;
using DelphiModernizer.Studio.UI.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace DelphiModernizer.Studio.UI.Common;

/// <summary>The Claude Code dialog and what each of its buttons does, for every page that offers it.</summary>
public static class ClaudeCodeSetup
{
    private const string _download = "https://claude.com/claude-code";

    /// <returns>Claude Code's status afterwards, so the caller redraws what it shows.</returns>
    public static async Task<ClaudeStatus> ShowAsync(XamlRoot root, ClaudeStatus status)
    {
        var services = ContractServices.Provider;
        var dialog = new ClaudeCodeDialog(status).Over(root);
        var choice = await dialog.ShowAsync();

        switch (choice)
        {
            case ContentDialogResult.None:
                return status;
            case ContentDialogResult.Primary when !dialog.IsUpdate:
                await Launcher.LaunchUriAsync(new Uri(_download));

                return status;
            case ContentDialogResult.Secondary when dialog.IsUpdate:
                var navigation = services.GetRequiredService<INavigationService>();
                navigation.NavigateTo("Settings");

                return status;
            case ContentDialogResult.Secondary:
                await ChooseExecutableAsync(services);
                break;
        }

        var sender = services.GetRequiredService<ISender>();

        return await sender.Send(new GetClaudeStatusQuery());
    }

    private static async Task ChooseExecutableAsync(IServiceProvider services)
    {
        var picker = services.GetRequiredService<IPickerService>();

        if (await picker.PickFileAsync(".exe") is { } path)
        {
            var settings = services.GetRequiredService<IStudioSettings>();
            settings.SaveAgents(settings.Agents with { ClaudePath = path });
        }
    }
}
