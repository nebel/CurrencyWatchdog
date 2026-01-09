using CurrencyWatchdog.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CurrencyWatchdog;

public sealed class AlertUpdater : IDisposable {
    private static readonly AlertComparer AlertComparer = new();

    private readonly Evaluator evaluator;
    private readonly InventoryWatcher inventoryWatcher;
    private readonly ZoneWatcher zoneWatcher;

    private AlertId[] currentChatAlertIds = [];
    private Alert[] currentPanelAlerts = [];

    private bool Enabled {
        get;
        set {
            if (field == value) return;
            field = value;

            if (value) {
                inventoryWatcher.OnChange += OnInventoryChange;
                zoneWatcher.OnChange += OnZoneChange;
            } else {
                inventoryWatcher.OnChange -= OnInventoryChange;
                zoneWatcher.OnChange -= OnZoneChange;
            }
        }
    } = false;

    public AlertUpdater(Evaluator evaluator) {
        this.evaluator = evaluator;
        inventoryWatcher = new InventoryWatcher();
        zoneWatcher = new ZoneWatcher();

        Plugin.ConfigManager.OnChange += OnConfigChange;
    }

    public void Dispose() {
        Enabled = false;
        inventoryWatcher.Dispose();
    }

    private void OnConfigChange(Config config) {
        Enabled = config.Enabled;

        if (!Enabled || !config.OverlayConfig.Enabled) {
            Plugin.Overlay.ClearNodes();
            return;
        }

        Plugin.Overlay.UpdateConfig(config);

        CheckAlertState(Reason.ConfigChange, OverlayRedrawMode.ForceRedraw, ChatAlertMode.Suppress);
    }

    private void OnZoneChange(ZoneWatcher.ChangeType zoneType) {
        if (!Enabled) return;

        switch (zoneType) {
            case ZoneWatcher.ChangeType.Login:
                CheckAlertState(Reason.Login, OverlayRedrawMode.ForceRedraw, ChatAlertMode.Suppress);
                return;
            case ZoneWatcher.ChangeType.LoginZoned:
                CheckAlertState(Reason.LoginZoned, OverlayRedrawMode.Skip, GetChatAlertMode(Plugin.Config.ChatConfig.LoginAction));
                return;
            case ZoneWatcher.ChangeType.TerritoryChange:
                // Do nothing, wait for zoned
                return;
            case ZoneWatcher.ChangeType.TerritoryChangeZoned:
                CheckAlertState(Reason.TerritoryChangeZoned, OverlayRedrawMode.Skip, GetChatAlertMode(Plugin.Config.ChatConfig.ZoneAction));
                return;
            default:
                throw new ArgumentOutOfRangeException($"Unknown ZoneWatcher.ChangeType: {zoneType}");
        }
    }

    private void OnInventoryChange(InventoryWatcher.ChangeType changeType) {
        var reason = changeType == InventoryWatcher.ChangeType.Inventory
            ? Reason.InventoryChange
            : Reason.CurrencyManagerChange;

        CheckAlertState(reason, OverlayRedrawMode.RedrawIfChanged, GetChatAlertMode(Plugin.Config.ChatConfig.AlertUpdateAction));
    }

    public void ResendActiveChatAlerts() {
        CheckAlertState(Reason.DebugResendAlerts, OverlayRedrawMode.Skip, ChatAlertMode.SendAll);
    }

    private void ResetAll() {
        if (currentPanelAlerts.Length > 0)
            Plugin.Overlay.UpdateNodes([]);
        currentPanelAlerts = [];
        currentChatAlertIds = [];
    }

    private void CheckAlertState(Reason reason, OverlayRedrawMode overlayRedrawMode, ChatAlertMode chatAlertMode) {
        // Service.Log.Warning($"Checking alerts [{reason}] (overlay:{overlayRedrawMode} chat:{chatAlertMode})");

        if (!Service.ClientState.IsLoggedIn || !Service.PlayerState.IsLoaded) {
            // Service.Log.Info("  Resetting alerts (not logged in)");
            ResetAll();
            return;
        }

        var (panelAlerts, chatAlerts) = evaluator.Evaluate(Plugin.Config.Burdens);

        // Chat

        var newChatAlerts = new List<Alert>();
        foreach (var alert in chatAlerts) {
            if (!currentChatAlertIds.Contains(alert.AlertId)) {
                newChatAlerts.Add(alert);
            }
        }
        currentChatAlertIds = chatAlerts.Select(a => a.AlertId).ToArray();

        if (chatAlertMode == ChatAlertMode.SendAll)
            Chat.SendChatAlerts(chatAlerts);
        else if (chatAlertMode == ChatAlertMode.SendNew)
            Chat.SendChatAlerts(newChatAlerts);

        // Panels

        if (overlayRedrawMode != OverlayRedrawMode.Skip) {
            var redraw = overlayRedrawMode == OverlayRedrawMode.ForceRedraw;

            if (!redraw && panelAlerts.Count != currentPanelAlerts.Length)
                redraw = true;

            if (!redraw && !panelAlerts.SequenceEqual(currentPanelAlerts, AlertComparer))
                redraw = true;

            // Service.Log.Info($"  Eval {Plugin.Config.Burdens.Count} burdens -> {panelAlerts.Count} panels (redraw:{redraw})");

            if (redraw) {
                currentPanelAlerts = panelAlerts.ToArray();
                Plugin.Overlay.UpdateNodes(panelAlerts);
            }
        }
    }

    private static ChatAlertMode GetChatAlertMode(ChatAlertUpdateAction action) {
        return Plugin.Config.ChatConfig.AlertUpdateAction switch {
            ChatAlertUpdateAction.None => ChatAlertMode.Suppress,
            ChatAlertUpdateAction.All => ChatAlertMode.SendAll,
            ChatAlertUpdateAction.New => ChatAlertMode.SendNew,
            _ => throw new ArgumentOutOfRangeException($"Unknown ChatAlertUpdateAction: {action}"),
        };
    }

    private static ChatAlertMode GetChatAlertMode(ChatAlertZoneAction action) {
        return Plugin.Config.ChatConfig.ZoneAction switch {
            ChatAlertZoneAction.None => ChatAlertMode.Suppress,
            ChatAlertZoneAction.All => ChatAlertMode.SendAll,
            _ => throw new ArgumentOutOfRangeException($"Unknown ChatAlertZoneAction: {action}"),
        };
    }

    private enum Reason {
        ConfigChange,
        InventoryChange,
        CurrencyManagerChange,
        Login,
        LoginZoned,
        TerritoryChangeZoned,
        DebugResendAlerts,
    }

    private enum OverlayRedrawMode {
        Skip,
        ForceRedraw,
        RedrawIfChanged,
    }

    private enum ChatAlertMode {
        SendAll,
        SendNew,
        Suppress,
    }
}
