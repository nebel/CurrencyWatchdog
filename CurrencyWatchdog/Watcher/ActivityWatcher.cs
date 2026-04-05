using Dalamud.Game.ClientState.Conditions;
using System;

namespace CurrencyWatchdog.Watcher;

public sealed class ActivityWatcher : IDisposable {
    private const ConditionFlag DutyFlag = ConditionFlag.BoundByDuty56;

    public event Action? OnChange;

    public ActivityWatcher() {
        Service.Condition.ConditionChange += OnConditionChange;
    }

    public void Dispose() {
        Service.Condition.ConditionChange -= OnConditionChange;
    }

    public static bool IsInDuty() => Service.Condition[DutyFlag];

    private void OnConditionChange(ConditionFlag flag, bool value) {
        if (flag == DutyFlag)
            Notify();
    }

    private void Notify() {
        OnChange?.Invoke();
    }
}
