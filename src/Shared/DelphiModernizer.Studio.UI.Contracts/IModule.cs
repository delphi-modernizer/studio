using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DelphiModernizer.Studio.UI.Contracts;

/// <summary>A feature module: it registers its own services and contributes pages to the shell; modules never reference one another.</summary>
public interface IModule
{
    string Name { get; }

    void RegisterServices(IServiceCollection services);

    IReadOnlyList<NavigationItem> GetNavigationItems();

    /// <summary>Pages reached from the module's own pages rather than the navigation pane, such as Run.</summary>
    IReadOnlyList<Type> GetPages()
    {
        return [];
    }
}
