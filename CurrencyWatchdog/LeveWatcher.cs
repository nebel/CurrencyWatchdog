using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using System;

namespace CurrencyWatchdog;

public sealed class LeveWatcher : IDisposable {
    public event Action? OnChange;

    [Signature("48 83 EC 28 8B 01 89 05 ?? ?? ?? ??", DetourName = nameof(SetLeveAllowanceDetour))]
    private readonly Hook<SetLeveAllowanceDelegate>? setLeveAllowanceHook = null!;

    private delegate nint SetLeveAllowanceDelegate(nint arg);

    private unsafe nint SetLeveAllowanceDetour(nint arg) {
        var origResult = setLeveAllowanceHook!.Original(arg);

        Service.Log.Debug($"Leve allowance set to {*(byte*)(arg + 4)}");
        OnChange?.Invoke();

        return origResult;
    }

    public LeveWatcher() {
        Service.GameInterop.InitializeFromAttributes(this);

        if (setLeveAllowanceHook is null) {
            Service.Log.Warning("Unable to find signature for leve allowance change");
            return;
        }

        setLeveAllowanceHook.Enable();
    }

    public void Dispose() {
        setLeveAllowanceHook?.Dispose();
        OnChange = null;
    }
}
