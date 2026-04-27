using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Watcher;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CurrencyWatchdog;

public sealed class AlertUpdater : IDisposable {
    private readonly Evaluator evaluator;
    private readonly InventoryWatcher inventoryWatcher;
    private readonly ZoneWatcher zoneWatcher;
    private readonly LeveWatcher leveWatcher;
    private readonly ConditionWatcher conditionWatcher;
    private readonly ActivityWatcher activityWatcher;

    private readonly ChatUpdater chatUpdater;
    private readonly OverlayUpdater overlayUpdater;

    private AlertState? alertState;

    private bool Enabled {
        get;
        set {
            if (field == value) return;
            field = value;

            if (value) {
                inventoryWatcher.OnChange += OnInventoryChange;
                zoneWatcher.OnChange += OnZoneChange;
                leveWatcher.OnChange += OnLeveChange;
                conditionWatcher.OnChange += OnConditionChange;
                activityWatcher.OnChange += OnActivityChange;
            } else {
                inventoryWatcher.OnChange -= OnInventoryChange;
                zoneWatcher.OnChange -= OnZoneChange;
                leveWatcher.OnChange -= OnLeveChange;
                conditionWatcher.OnChange -= OnConditionChange;
                activityWatcher.OnChange -= OnActivityChange;
            }
        }
    } = false;

    public AlertUpdater(Evaluator evaluator) {
        this.evaluator = evaluator;
        inventoryWatcher = new InventoryWatcher();
        zoneWatcher = new ZoneWatcher();
        leveWatcher = new LeveWatcher();
        conditionWatcher = new ConditionWatcher();
        activityWatcher = new ActivityWatcher();

        chatUpdater = new ChatUpdater(zoneWatcher);
        overlayUpdater = new OverlayUpdater();

        Plugin.ConfigManager.OnChange += OnConfigChange;
    }

    public void Dispose() {
        Enabled = false;
        inventoryWatcher.Dispose();
        zoneWatcher.Dispose();
        leveWatcher.Dispose();
        conditionWatcher.Dispose();
    }

    private void OnConfigChange(Config config) {
        Enabled = config.Enabled;

        if (!Enabled || !config.OverlayConfig.Enabled) {
            Plugin.Overlay.ClearNodes();
            return;
        }

        Plugin.Overlay.UpdateConfig(config);

        CheckAlertState(UpdateReason.ConfigChange);
    }

    private void OnZoneChange(ZoneWatcher.ChangeType zoneType) {
        if (!Enabled) return;

        switch (zoneType) {
            case ZoneWatcher.ChangeType.Login:
                CheckAlertState(UpdateReason.Login);
                return;
            case ZoneWatcher.ChangeType.LoginZoned:
                CheckAlertState(UpdateReason.LoginZoned);
                return;
            case ZoneWatcher.ChangeType.TerritoryChange:
                // Do nothing, wait for zoned
                return;
            case ZoneWatcher.ChangeType.TerritoryChangeZoned:
                CheckAlertState(UpdateReason.TerritoryChangeZoned);
                return;
            default:
                throw new ArgumentOutOfRangeException($"Unknown ZoneWatcher.ChangeType: {zoneType}");
        }
    }

    private void OnInventoryChange(InventoryWatcher.ChangeType changeType) {
        Service.Log.Verbose($"Inventory change detected [{changeType}] ({Service.ClientState.IsLoggedIn}/{Service.PlayerState.IsLoaded})");

        var reason = changeType == InventoryWatcher.ChangeType.Inventory
            ? UpdateReason.InventoryChange
            : UpdateReason.CurrencyManagerChange;

        CheckAlertState(reason);
    }


    private void OnLeveChange() {
        CheckAlertState(UpdateReason.LeveChange);
    }

    private void OnConditionChange() {
        CheckAlertState(UpdateReason.ConditionChange);
    }

    private void OnActivityChange() {
        CheckAlertState(UpdateReason.ActivityChange);
    }

    public void ResendActiveChatAlerts() {
        CheckAlertState(UpdateReason.DebugResendAlerts);
    }

    private void ResetAll() {
        chatUpdater.Reset();
        overlayUpdater.Reset();
    }

    private void CheckAlertState(UpdateReason reason) {
        Service.Log.Verbose($"Checking alerts [{reason}]");

        if (!Service.ClientState.IsLoggedIn || !Service.PlayerState.IsLoaded) {
            Service.Log.Verbose("  Resetting alerts (not logged in)");
            ResetAll();
            return;
        }

        if (ShouldEvaluate(reason)) {
            Service.Log.Verbose("  Evaluating alerts");
            alertState = evaluator.Evaluate(Plugin.Config.Burdens);
        } else if (alertState is null) {
            Service.Log.Verbose("  Evaluating alerts (empty cache)");
            alertState = evaluator.Evaluate(Plugin.Config.Burdens);
        } else {
            Service.Log.Verbose("  Using cached alerts");
        }

        chatUpdater.Update(reason, alertState.ChatAlerts);
        overlayUpdater.Update(reason, alertState.PanelAlerts);
    }

    private static bool ShouldEvaluate(UpdateReason reason) {
        return reason is
            UpdateReason.ConfigChange
            or UpdateReason.InventoryChange
            or UpdateReason.CurrencyManagerChange
            or UpdateReason.LeveChange
            or UpdateReason.Login;
    }
}

public enum UpdateReason {
    ConfigChange,
    InventoryChange,
    CurrencyManagerChange,
    LeveChange,
    ConditionChange,
    ActivityChange,
    Login,
    LoginZoned,
    TerritoryChangeZoned,
    DebugResendAlerts,
}

public class ChatUpdater(ZoneWatcher zoneWatcher) {
    private AlertId[] currentChatAlertIds = [];

    public void Update(UpdateReason reason, List<Alert> alerts) {
        var newChatAlerts = new List<Alert>();
        foreach (var alert in alerts) {
            if (!currentChatAlertIds.Contains(alert.AlertId)) {
                newChatAlerts.Add(alert);
            }
        }
        currentChatAlertIds = alerts.Select(a => a.AlertId).ToArray();

        var notifyMode = GetNotifyMode(reason);
        Service.Log.Verbose($"  Chat: {notifyMode}");

        if (notifyMode == NotifyMode.SendAll)
            Chat.SendChatAlerts(alerts);
        else if (notifyMode == NotifyMode.SendNew)
            Chat.SendChatAlerts(newChatAlerts);
    }

    public void Reset() {
        currentChatAlertIds = [];
    }

    private NotifyMode GetNotifyMode(UpdateReason reason) {
        switch (reason) {
            case UpdateReason.ConfigChange:
                return NotifyMode.Suppress;

            case UpdateReason.InventoryChange:
            case UpdateReason.CurrencyManagerChange:
            case UpdateReason.LeveChange:
                if (zoneWatcher.LoginState != ZoneWatcher.LoginStateType.Complete)
                    return NotifyMode.Suppress;
                return FromUpdateAction(Plugin.Config.ChatConfig.AlertUpdateAction);

            case UpdateReason.ConditionChange:
            case UpdateReason.ActivityChange:
                return NotifyMode.Suppress;

            case UpdateReason.Login:
                return NotifyMode.Suppress;

            case UpdateReason.LoginZoned:
                return FromZoneAction(Plugin.Config.ChatConfig.LoginAction);

            case UpdateReason.TerritoryChangeZoned:
                if (zoneWatcher.LoginState != ZoneWatcher.LoginStateType.Complete)
                    return NotifyMode.Suppress;
                return FromZoneAction(Plugin.Config.ChatConfig.ZoneAction);

            case UpdateReason.DebugResendAlerts:
                return NotifyMode.SendAll;

            default:
                throw new ArgumentOutOfRangeException($"Unknown UpdateReason: {reason}");
        }
    }

    private static NotifyMode FromZoneAction(ChatAlertZoneAction action) {
        return action switch {
            ChatAlertZoneAction.None => NotifyMode.Suppress,
            ChatAlertZoneAction.All => NotifyMode.SendAll,
            _ => throw new ArgumentOutOfRangeException($"Unknown ChatAlertZoneAction: {action}"),
        };
    }

    private static NotifyMode FromUpdateAction(ChatAlertUpdateAction action) {
        return action switch {
            ChatAlertUpdateAction.None => NotifyMode.Suppress,
            ChatAlertUpdateAction.All => NotifyMode.SendAll,
            ChatAlertUpdateAction.New => NotifyMode.SendNew,
            _ => throw new ArgumentOutOfRangeException($"Unknown ChatAlertUpdateAction: {action}"),
        };
    }

    private enum NotifyMode {
        SendAll,
        SendNew,
        Suppress,
    }
}

public class OverlayUpdater {
    private Alert[] currentPanelAlerts = [];

    public void Update(UpdateReason reason, List<Alert> alerts) {
        var redrawMode = GetRedrawMode(reason);
        Service.Log.Verbose($"  Overlay: {redrawMode}");

        if (redrawMode == RedrawMode.Skip) return;

        var redraw = redrawMode == RedrawMode.ForceRedraw;

        var shownAlerts = new List<Alert>();
        var defaultVis = GetDefaultVisibility();
        foreach (var alert in alerts) {
            if (GetAlertVisibility(alert, defaultVis) == PanelVisibilityKind.Show)
                shownAlerts.Add(alert);
        }

        if (!redraw && shownAlerts.Count != currentPanelAlerts.Length)
            redraw = true;

        if (!redraw && !shownAlerts.SequenceEqual(currentPanelAlerts, AlertComparer.Instance))
            redraw = true;

        Service.Log.Verbose($"    Eval {Plugin.Config.Burdens.Count} burdens -> {shownAlerts.Count} panels (redraw:{redraw})");

        if (redraw) {
            currentPanelAlerts = shownAlerts.ToArray();
            Plugin.Overlay.UpdateNodes(shownAlerts);
        }
    }

    private static DefaultPanelVisibility GetDefaultVisibility() {
        return new DefaultPanelVisibility {
            Default = PanelVisibilityKind.Show,
            InDuty = Plugin.Config.OverlayConfig.HideInDuty ? PanelVisibilityKind.Hide : PanelVisibilityKind.Show,
        };
    }

    private static PanelVisibilityKind GetAlertVisibility(Alert alert, DefaultPanelVisibility defaultVis) {
        if (alert.Jurisdictions.Length != 0) {
            foreach (var juris in alert.Jurisdictions) {
                if (juris.Enabled && GetJurisdictionVisibility(juris) is { } visibility) {
                    if (ConditionWatcher.IsInDuty())
                        return visibility.InDuty ?? defaultVis.InDuty;
                    return visibility.Default ?? defaultVis.Default;
                }
            }
        }

        if (ConditionWatcher.IsInDuty())
            return defaultVis.InDuty;
        return defaultVis.Default;
    }

    public static PanelVisibility? GetJurisdictionVisibility(Jurisdiction juris) {
        if (juris.Activities.Count == 0)
            return juris.Visibility;

        foreach (var activity in juris.Activities) {
            if (!activity.Enabled) continue;
            switch (activity) {
                case Activity.ContentActivity contentActivity:
                    if (contentActivity.RowId == ActivityWatcher.CurrentContentFinderConditionId)
                        return juris.Visibility;
                    break;

                case Activity.ZoneActivity zoneActivity:
                    if (zoneActivity.RowId == ActivityWatcher.CurrentTerritoryTypeId)
                        return juris.Visibility;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown activity type: {activity.GetType().Name}");
            }
        }

        return null;
    }

    public void Reset() {
        if (currentPanelAlerts.Length > 0)
            Plugin.Overlay.UpdateNodes([]);
        currentPanelAlerts = [];
    }

    private RedrawMode GetRedrawMode(UpdateReason reason) {
        switch (reason) {
            case UpdateReason.ConfigChange:
                return RedrawMode.ForceRedraw;

            case UpdateReason.InventoryChange:
            case UpdateReason.CurrencyManagerChange:
            case UpdateReason.LeveChange:
                return RedrawMode.RedrawIfChanged;

            case UpdateReason.ConditionChange:
            case UpdateReason.ActivityChange:
                return RedrawMode.RedrawIfChanged;

            case UpdateReason.Login:
                return RedrawMode.ForceRedraw;

            case UpdateReason.LoginZoned:
                return RedrawMode.Skip;

            case UpdateReason.TerritoryChangeZoned:
                return RedrawMode.Skip;

            case UpdateReason.DebugResendAlerts:
                return RedrawMode.Skip;

            default:
                throw new ArgumentOutOfRangeException($"Unknown UpdateReason: {reason}");
        }
    }

    private enum RedrawMode {
        Skip,
        ForceRedraw,
        RedrawIfChanged,
    }

    private ref struct DefaultPanelVisibility {
        public PanelVisibilityKind Default;
        public PanelVisibilityKind InDuty;
    }
}
