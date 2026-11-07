using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DelphiModernizer.Studio.Application.Abstractions;
using DelphiModernizer.Studio.UI.Contracts.Services;

namespace DelphiModernizer.Studio.App.Services;

/// <summary>Keeps small settings as one JSON file under <see cref="StudioPaths.DataDirectory"/>.</summary>
/// <remarks>
/// An unreadable file starts fresh and an unwritable one is skipped: settings must never stop the app.
/// </remarks>
public sealed class LocalSettingsService : ILocalSettingsService
{
    private readonly string _path = Path.Combine(StudioPaths.DataDirectory, "settings.json");
    private readonly Dictionary<string, JsonElement> _values;

    public LocalSettingsService()
    {
        _values = Load(_path);
    }

    public T? Read<T>(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            return _values.TryGetValue(key, out var value) ? value.Deserialize<T>() : default;
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public void Save<T>(string key, T value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _values[key] = JsonSerializer.SerializeToElement(value);

        try
        {
            Directory.CreateDirectory(StudioPaths.DataDirectory);
            File.WriteAllText(_path, JsonSerializer.Serialize(_values));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The value stays in memory for this session.
        }
    }

    private static Dictionary<string, JsonElement> Load(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(path)) ?? []
                : [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }
}
