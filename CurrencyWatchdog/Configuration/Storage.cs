using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CurrencyWatchdog.Configuration;

#pragma warning disable Dalamud001
#pragma warning disable CS8618 // Injected properties
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class Storage {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        WriteIndented = true,
        IncludeFields = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [PluginService] private IReliableFileStorage ReliableFileStorage { get; set; }

    [PluginService] private IPluginLog Log { get; set; }

    private readonly IDalamudPluginInterface pluginInterface;

    public Storage(IDalamudPluginInterface pluginInterface) {
        this.pluginInterface = pluginInterface;
        pluginInterface.Inject(this);
    }

    public void Save<T>(string relativePath, T value) {
        if (value is null) throw new InvalidOperationException("Cannot save null data.");
        var path = Path.Join(pluginInterface.GetPluginConfigDirectory(), relativePath);
        try {
            var json = JsonSerializer.Serialize(value, value.GetType(), SerializerOptions);
            ReliableFileStorage.WriteAllTextAsync(path, json).GetAwaiter().GetResult();
        } catch (Exception e) {
            Log.Error(e, $"Failed to serialize/write file: {path}");
        }
    }

    public T? Load<T>(string relativePath) {
        var path = Path.Join(pluginInterface.GetPluginConfigDirectory(), relativePath);

        if (!File.Exists(path)) {
            Log.Info($"File does not exist: {path}");
            return default;
        }

        try {
            var json = ReliableFileStorage.ReadAllTextAsync(path).GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<T>(json, SerializerOptions);
        } catch (Exception e) {
            Log.Error(e, $"Failed to read/deserialize file: {path}");
            return default;
        }
    }
}

public record StorageContext<T> where T : new() {
    public string Name;
    public string Profile;
    public bool AllowSave = true;
    public bool AllowLoad = true;
    public Func<T>? Fallback;

    private string Path => $"{Name}.{Profile}.json";

    public StorageContext(string name, string profile) {
        Name = name;
        Profile = profile;
    }

    public T GetDefault() {
        if (Fallback is { } supplier)
            return supplier();
        return new T();
    }

    public void Save(Storage storage, T value) {
        if (AllowSave)
            storage.Save(Path, value);
    }

    public T Load(Storage storage) {
        if (AllowLoad)
            return storage.Load<T>(Path) ?? GetDefault();
        return GetDefault();
    }
}
