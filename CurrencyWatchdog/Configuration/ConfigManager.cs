using CurrencyWatchdog.Utility;
using System;

namespace CurrencyWatchdog.Configuration;

public class ConfigManager {
    public Config Current = new();

    public event Action<Config>? OnChange;

    private readonly Storage storage;
    private readonly StorageContext<Config> storageContext;

    public ConfigManager() {
        storage = new Storage(Service.PluginInterface);
        storageContext = new StorageContext<Config>("Config", "Default");

        if (Debugging.GetDebugStorage() is { } context) {
            storageContext = context;
        }
    }

    public void Load() {
        Current = storageContext.Load(storage);
        OnChange?.Invoke(Current);
    }

    public void LoadObject(Config config) {
        Current = config;
        Save();
    }

    public void FullReset() {
        Current = storageContext.GetDefault();
        OnChange?.Invoke(Current);
    }

    public void Save() {
        storageContext.Save(storage, Current);
        OnChange?.Invoke(Current);
    }

}
