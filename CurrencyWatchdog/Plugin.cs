using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Interface;
using CurrencyWatchdog.Native;
using CurrencyWatchdog.Utility;
using Dalamud.Plugin;
using KamiToolKit;
using System.Threading;
using System.Threading.Tasks;

namespace CurrencyWatchdog;

public sealed class Plugin : IAsyncDalamudPlugin {
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
    }

    public async Task LoadAsync(CancellationToken cancellationToken) {
        await KamiToolKitLibrary.InitializeAsync(Service.PluginInterface);

        ConfigManager = new ConfigManager();

        CommandManager = new CommandManager();
        WindowManager = new WindowManager();

        Overlay = new Overlay();
        Evaluator = new Evaluator();
        AlertUpdater = new AlertUpdater(Evaluator);

        await Service.Framework.Run(() => {
            Overlay.FrameworkThreadInit();
            ConfigManager.Load();

            Debugging.PostInit();
        }, cancellationToken);
    }

    public async ValueTask DisposeAsync() {
        WindowManager.Dispose();
        AlertUpdater.Dispose();

        await Service.Framework.Run(async () => {
            Overlay.Dispose();

            await KamiToolKitLibrary.DisposeAsync();
        });
    }

}
