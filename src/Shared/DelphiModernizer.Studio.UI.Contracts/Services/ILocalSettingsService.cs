namespace DelphiModernizer.Studio.UI.Contracts.Services;

/// <summary>Small per-user values kept as JSON under local app data, which the unpackaged app can always write.</summary>
public interface ILocalSettingsService
{
    T? Read<T>(string key);

    void Save<T>(string key, T value);
}
