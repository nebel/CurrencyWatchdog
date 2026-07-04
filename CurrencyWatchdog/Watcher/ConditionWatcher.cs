using Dalamud.Game.ClientState.Conditions;
using System;

namespace CurrencyWatchdog.Watcher;

public sealed class ConditionWatcher : IDisposable {
    private const ConditionFlag DutyFlag = ConditionFlag.BoundByDuty56;
    private const ConditionFlag CombatFlag = ConditionFlag.InCombat;

    public event Action? OnChange;

    public ConditionWatcher() {
        Service.Condition.ConditionChange += OnConditionChange;
    }

    public void Dispose() {
        Service.Condition.ConditionChange -= OnConditionChange;
    }

    public static bool IsInDuty() => Service.Condition[DutyFlag];

    public static bool IsInCombat() => Service.Condition[CombatFlag];

    private void OnConditionChange(ConditionFlag flag, bool value) {
        if (flag is DutyFlag or CombatFlag)
            Notify();
    }

    private void Notify() {
        OnChange?.Invoke();
    }
}
