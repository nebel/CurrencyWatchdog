using Dalamud.Game.ClientState.Conditions;
using System;

namespace CurrencyWatchdog;

public sealed class ZoneWatcher : IDisposable {
    private bool waiting;
    private bool waitingForLoginZone;
    private bool waitingForTerritoryZone;

    public LoginStateType LoginState;

    public event Action<ChangeType>? OnChange;

    public ZoneWatcher() {
        LoginState = Service.ClientState.IsLoggedIn ? LoginStateType.Complete : LoginStateType.Zoning;

        Service.ClientState.Login += OnLogin;
        Service.ClientState.Logout += OnLogout;
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
            LoginState = LoginStateType.Zoning;
            waitingForLoginZone = true;
            StartWait();
        } else {
            LoginState = LoginStateType.Complete;
            Notify(ChangeType.LoginZoned);
        }
    }

    private void OnLogout(int type, int code) {
        LoginState = LoginStateType.None;
    }

    private void OnTerritoryChanged(ushort id) {
        Notify(ChangeType.TerritoryChange);

        if (IsZoning()) {
            waitingForTerritoryZone = true;
            StartWait();
        } else {
            Notify(ChangeType.TerritoryChangeZoned);
        }
    }

    private void StartWait() {
        if (!waiting) {
            waiting = true;
            Service.Condition.ConditionChange += OnConditionChange;
        }
    }

    private void OnConditionChange(ConditionFlag flag, bool value) {
        if (flag == ConditionFlag.BetweenAreas && !value)
            CompleteWait();
    }

    private void CompleteWait() {
        var isResolving = false;

        if (waitingForLoginZone) {
            LoginState = LoginStateType.Resolving;
            isResolving = true;

            waitingForLoginZone = false;
            Notify(ChangeType.LoginZoned);
        }

        if (waitingForTerritoryZone) {
            waitingForTerritoryZone = false;
            Notify(ChangeType.TerritoryChangeZoned);
        }

        if (isResolving)
            LoginState = LoginStateType.Complete;

        Service.Condition.ConditionChange -= OnConditionChange;
        waiting = false;
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

    public enum LoginStateType {
        None,
        Zoning,
        Resolving,
        Complete,
    }
}
