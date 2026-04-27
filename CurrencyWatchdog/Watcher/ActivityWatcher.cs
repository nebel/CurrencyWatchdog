using Dalamud.Game.ClientState;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using Action = System.Action;

namespace CurrencyWatchdog.Watcher;

public sealed class ActivityWatcher : IDisposable {
    public static uint CurrentTerritoryTypeId { get; private set; }
    public static uint CurrentContentFinderConditionId { get; private set; }

    public event Action? OnChange;

    public ActivityWatcher() {
        Init();
        Service.ClientState.ZoneInit += ZoneInit;
    }

    public void Dispose() {
        Service.ClientState.ZoneInit -= ZoneInit;
    }

    private void Init() {
        Update(GetCurrentTerritoryTypeId(), GetContentFinderConditionId());
    }

    private void ZoneInit(ZoneInitEventArgs args) {
        if (Update(args.TerritoryType.RowId, args.ContentFinderCondition.RowId))
            OnChange?.Invoke();
    }

    private bool Update(uint territoryTypeId, uint contentFinderConditionId) {
        var changed = false;

        if (CurrentTerritoryTypeId != territoryTypeId) {
            CurrentTerritoryTypeId = territoryTypeId;
            changed = true;
        }

        if (CurrentContentFinderConditionId != contentFinderConditionId) {
            CurrentContentFinderConditionId = contentFinderConditionId;
            changed = true;
        }

        return changed;
    }

    private static unsafe uint GetCurrentTerritoryTypeId() {
        var current = GameMain.Instance()->CurrentTerritoryTypeId;
        return current == 0 ? GameMain.Instance()->NextTerritoryTypeId : current;
    }

    private static unsafe uint GetContentFinderConditionId() {
        return GameMain.Instance()->CurrentContentFinderConditionId;
    }
}
