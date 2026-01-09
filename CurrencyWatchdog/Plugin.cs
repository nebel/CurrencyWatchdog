using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Interface;
using CurrencyWatchdog.Native;
using CurrencyWatchdog.Utility;
using Dalamud.Plugin;
using KamiToolKit;

namespace CurrencyWatchdog;

public sealed class Plugin : IDalamudPlugin {
    public const string Name = "Currency Watchdog";

    public static ConfigManager ConfigManager { get; private set; } = null!;
    public static Config Config => ConfigManager.Current;
    public static CommandManager CommandManager { get; private set; } = null!;
    public static WindowManager WindowManager { get; private set; } = null!;
    public static Overlay Overlay { get; private set; } = null!;
    public static Evaluator Evaluator { get; private set; } = null!;
    public static AlertUpdater AlertUpdater { get; private set; } = null!;

    public Plugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Service>();
        KamiToolKitLibrary.Initialize(pluginInterface);

        ConfigManager = new ConfigManager();

        CommandManager = new CommandManager();
        WindowManager = new WindowManager();

        Overlay = new Overlay();
        Evaluator = new Evaluator();
        AlertUpdater = new AlertUpdater(Evaluator);

        Service.Framework.RunOnFrameworkThread(() => {
            Overlay.FrameworkThreadInit();
            ConfigManager.Load();

            Debugging.PostInit();
        });
    }

    public void Dispose() {
        WindowManager.Dispose();
        AlertUpdater.Dispose();
        Overlay.Dispose();
        KamiToolKitLibrary.Dispose();
    }
}
