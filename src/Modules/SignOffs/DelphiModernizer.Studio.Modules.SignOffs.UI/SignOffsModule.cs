using System;
using System.Collections.Generic;
using DelphiModernizer.Studio.Modules.SignOffs.Application;
using DelphiModernizer.Studio.Modules.SignOffs.UI.ViewModels;
using DelphiModernizer.Studio.Modules.SignOffs.UI.Views;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.SignOffs.UI;

/// <summary>The Sign-offs module: its page and the services behind it.</summary>
public sealed class SignOffsModule : IModule
{
    public string Name => "SignOffs";

    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSignOffsApplication();
        services.AddTransient<SignOffsViewModel>();
        services.AddTransient<SignOffsPage>();
    }

    public IReadOnlyList<NavigationItem> GetNavigationItems()
    {
        return [
            new NavigationItem("Sign-offs", StudioIcons.SignOffs, StudioIcons.SignOffsFilled, typeof(SignOffsPage), 60),
        ];
    }
}
