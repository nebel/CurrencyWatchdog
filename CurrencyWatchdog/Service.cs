using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System.Diagnostics.CodeAnalysis;

namespace CurrencyWatchdog;

#pragma warning disable CS8618 // Injected properties
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class Service {
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; }
    [PluginService] public static ICommandManager CommandManager { get; set; }
    [PluginService] public static IPluginLog Log { get; set; }
    [PluginService] public static IDataManager DataManager { get; set; }
    [PluginService] public static ITextureProvider TextureProvider { get; set; }
    [PluginService] public static IFramework Framework { get; set; }
    [PluginService] public static IClientState ClientState { get; set; }
    [PluginService] public static IPlayerState PlayerState { get; set; }
    [PluginService] public static ICondition Condition { get; set; }
    [PluginService] public static IGameInventory GameInventory { get; set; }
    [PluginService] public static IChatGui ChatGui { get; set; }
    [PluginService] public static IGameInteropProvider GameInterop { get; set; }
}
