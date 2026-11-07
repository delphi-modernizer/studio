using System;
using System.Collections.Generic;
using DelphiModernizer.Studio.Modules.Settings.UI.ViewModels;
using DelphiModernizer.Studio.Modules.Settings.UI.Views;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Settings.UI;

/// <summary>The Settings module: its page and the services behind it.</summary>
public sealed class SettingsModule : IModule
{
    public string Name => "Settings";

    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsPage>();
    }

    public IReadOnlyList<NavigationItem> GetNavigationItems()
    {
        return [
            new NavigationItem("Settings", StudioIcons.Settings, StudioIcons.SettingsFilled, typeof(SettingsPage), 0) {
                Placement = NavigationPlacement.Footer,
            },
        ];
    }
}
