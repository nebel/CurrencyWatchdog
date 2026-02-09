using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;

namespace CurrencyWatchdog;

public sealed class InventoryWatcher : IDisposable {
    private readonly Hook<CurrencyManager.Delegates.SetItemData> setItemDataHook;

    public event Action<ChangeType>? OnChange;

    public unsafe InventoryWatcher() {
        Service.GameInventory.InventoryChanged += OnInventoryChanged;

        setItemDataHook = Service.GameInterop.HookFromAddress<CurrencyManager.Delegates.SetItemData>(
            (nint)CurrencyManager.MemberFunctionPointers.SetItemData, OnSetItemData);
        setItemDataHook.Enable();
    }

    private unsafe void OnSetItemData(CurrencyManager* thisPtr, sbyte specialId, uint itemId, uint maxCount, uint count, bool isUnlimited) {
        setItemDataHook.Original(thisPtr, specialId, itemId, maxCount, count, isUnlimited);
        OnChange?.Invoke(ChangeType.CurrencyManager);
    }

    private void OnInventoryChanged(IReadOnlyCollection<InventoryEventArgs> events) {
        OnChange?.Invoke(ChangeType.Inventory);
    }

    public void Dispose() {
        Service.GameInventory.InventoryChanged -= OnInventoryChanged;
        setItemDataHook.Dispose();
    }

    public enum ChangeType {
        Inventory,
        CurrencyManager,
    }
}
