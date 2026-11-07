using System;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DelphiModernizer.Studio.Tests;

/// <summary>A container wired the way the App wires a module, around a given session.</summary>
internal static class Services
{
    public static ISender Sender(IWorkspaceSession session, Action<IServiceCollection> addModule)
    {
        var services = new ServiceCollection();
        services.AddSingleton(session);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        addModule(services);

        return services
            .BuildServiceProvider()
            .GetRequiredService<ISender>();
    }
}
