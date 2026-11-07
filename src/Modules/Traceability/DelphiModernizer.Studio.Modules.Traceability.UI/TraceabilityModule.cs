using System;
using System.Collections.Generic;
using DelphiModernizer.Studio.Modules.Traceability.Application;
using DelphiModernizer.Studio.Modules.Traceability.UI.ViewModels;
using DelphiModernizer.Studio.Modules.Traceability.UI.Views;
using DelphiModernizer.Studio.UI.Common.Icons;
using DelphiModernizer.Studio.UI.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.Modules.Traceability.UI;

/// <summary>The Traceability module: its page and the services behind it.</summary>
public sealed class TraceabilityModule : IModule
{
    public string Name => "Traceability";

    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTraceabilityApplication();
        services.AddSingleton<ProblemsBadge>();
        services.AddTransient<TraceabilityViewModel>();
        services.AddTransient<TraceabilityPage>();
    }

    public IReadOnlyList<NavigationItem> GetNavigationItems()
    {
        return [
            new NavigationItem("Traceability", StudioIcons.Traceability, StudioIcons.TraceabilityFilled, typeof(TraceabilityPage), 50) {
                Badge = provider => provider.GetRequiredService<ProblemsBadge>(),
            },
        ];
    }
}
