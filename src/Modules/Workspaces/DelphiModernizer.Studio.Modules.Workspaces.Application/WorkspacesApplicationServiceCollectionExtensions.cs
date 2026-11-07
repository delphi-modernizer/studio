using System;
using System.IO;
using DelphiModernizer.Studio.Mediator;
using DelphiModernizer.Studio.Modules.Workspaces.Application.Recent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DelphiModernizer.Studio.Modules.Workspaces.Application;

public static class WorkspacesApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Adds the module's handlers and the recent-workspaces list under <paramref name="dataDirectory"/>.
    /// </summary>
    public static IServiceCollection AddWorkspacesApplication(this IServiceCollection services, string dataDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddMediator(typeof(WorkspacesApplicationServiceCollectionExtensions).Assembly);
        services.TryAddSingleton(new RecentWorkspaceStore(Path.Combine(dataDirectory, "recent-workspaces.json")));

        return services;
    }
}
