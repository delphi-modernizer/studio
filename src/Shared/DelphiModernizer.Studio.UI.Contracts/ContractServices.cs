using System;

namespace DelphiModernizer.Studio.UI.Contracts;

/// <summary>The service provider for controls XAML creates with <c>new()</c>, which cannot take constructor injection.</summary>
/// <remarks>Everything else uses constructor injection. The App configures this once, at startup.</remarks>
public static class ContractServices
{
    private static IServiceProvider? _provider;

    public static IServiceProvider Provider =>
        _provider ?? throw new InvalidOperationException("ContractServices.Configure has not been called.");

    public static void Configure(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }
}
