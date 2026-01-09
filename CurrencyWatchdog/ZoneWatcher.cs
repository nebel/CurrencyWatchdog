using Dalamud.Game.ClientState.Conditions;
using System;

namespace CurrencyWatchdog;

public sealed class ZoneWatcher : IDisposable {
    private ChangeType? waitType;

    public event Action<ChangeType>? OnChange;

    public ZoneWatcher() {
        Service.ClientState.Login += OnLogin;
        Service.ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void Dispose() {
        Service.ClientState.Login -= OnLogin;
        Service.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Service.Condition.ConditionChange -= OnConditionChange;
    }

    private static bool IsZoning() => Service.Condition[ConditionFlag.BetweenAreas];

    private void OnLogin() {
        Notify(ChangeType.Login);

        if (IsZoning()) {
            StartWait(ChangeType.LoginZoned);
        } else {
            Notify(ChangeType.LoginZoned);
        }
    }

    private void OnTerritoryChanged(ushort id) {
        Notify(ChangeType.TerritoryChange);

        if (IsZoning()) {
            StartWait(ChangeType.TerritoryChangeZoned);
        } else {
            Notify(ChangeType.TerritoryChangeZoned);
        }
    }

    private void OnConditionChange(ConditionFlag flag, bool value) {
        if (flag == ConditionFlag.BetweenAreas && !value)
            CompleteWait();
    }

    private void StartWait(ChangeType type) {
        if (waitType is null) {
            waitType = type;
            Service.Condition.ConditionChange += OnConditionChange;
        }
    }

    private void CompleteWait() {
        if (waitType is { } type) {
            waitType = null;
            Notify(type);
        }
        Service.Condition.ConditionChange -= OnConditionChange;
    }

    private void Notify(ChangeType type) {
        OnChange?.Invoke(type);
    }

    public enum ChangeType {
        Login,
        LoginZoned,
        TerritoryChange,
        TerritoryChangeZoned,
    }
}
