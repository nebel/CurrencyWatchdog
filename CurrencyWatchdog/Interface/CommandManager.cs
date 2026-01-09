using Dalamud.Game.Command;
using System.Text.RegularExpressions;

namespace CurrencyWatchdog.Interface;

#pragma warning disable SYSLIB1045 // Use pre-compiled regex (not necessary here)
public class CommandManager {
    private const string CommandName = "/cdog";

    public CommandManager() {
        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = "Open the settings window"
                          + $"\n{CommandName} plugin on|off|toggle → Enable, disable or toggle the plugin"
                          + $"\n{CommandName} overlay on|off|toggle → Enable, disable or toggle the overlay"
                          + $"\n{CommandName} chat on|off|toggle → Enable, disable or toggle chat alerts",
        });
    }

    private void OnCommand(string command, string arguments) {
        var args = Regex.Replace(arguments, @"\s+", " ").Trim();
        switch (args) {
            case "plugin on":
                Enable();
                break;
            case "plugin off":
                Disable();
                break;
            case "plugin toggle":
                Toggle();
                break;
            case "overlay on":
                EnableOverlay();
                break;
            case "overlay off":
                DisableOverlay();
                break;
            case "overlay toggle":
                ToggleOverlay();
                break;
            case "chat on":
                EnableChat();
                break;
            case "chat off":
                DisableChat();
                break;
            case "chat toggle":
                ToggleChat();
                break;
            case "":
                ToggleConfigUi();
                break;
            default:
                Service.ChatGui.PrintError($"Invalid subcommand \"{args}\".", Plugin.Name);
                break;
        }
    }

    private static void ToggleConfigUi() {
        Plugin.WindowManager.ConfigWindow.Toggle();
    }

    private static void Enable() {
        if (!Plugin.Config.Enabled) {
            Plugin.Config.Enabled = true;
            Plugin.ConfigManager.Save();
        }
    }

    private static void Disable() {
        if (Plugin.Config.Enabled) {
            Plugin.Config.Enabled = false;
            Plugin.ConfigManager.Save();
        }
    }

    private static void Toggle() {
        if (Plugin.Config.Enabled)
            Disable();
        else
            Enable();
    }

    private static void EnableOverlay() {
        if (!Plugin.Config.OverlayConfig.Enabled) {
            Plugin.Config.OverlayConfig.Enabled = true;
            Plugin.ConfigManager.Save();
        }
    }

    private static void DisableOverlay() {
        if (Plugin.Config.OverlayConfig.Enabled) {
            Plugin.Config.OverlayConfig.Enabled = false;
            Plugin.ConfigManager.Save();
        }
    }

    private static void ToggleOverlay() {
        if (Plugin.Config.OverlayConfig.Enabled)
            EnableOverlay();
        else
            DisableOverlay();
    }

    private static void EnableChat() {
        if (!Plugin.Config.ChatConfig.Enabled) {
            Plugin.Config.ChatConfig.Enabled = true;
            Plugin.ConfigManager.Save();
        }
    }

    private static void DisableChat() {
        if (Plugin.Config.ChatConfig.Enabled) {
            Plugin.Config.ChatConfig.Enabled = false;
            Plugin.ConfigManager.Save();
        }
    }

    private static void ToggleChat() {
        if (Plugin.Config.ChatConfig.Enabled)
            DisableChat();
        else
            EnableChat();
    }
}
