using CurrencyWatchdog.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;

namespace CurrencyWatchdog.Interface.Utility;

public sealed class ListDragDrop<T>(string payloadId) {
    private readonly DragDropState state = new(payloadId);

    private List<T>? SourceList { get; set; }
    private int? SourceIndex { get; set; }
    public string? SourceName { get; set; }
    public DragAction DragAction { get; set; } = DragAction.None;

    public void EndFrame() {
        if (!state.CanDrop())
            DragAction = DragAction.None;

        if (state.IsActive()) {
            DrawTooltip();
        } else {
            SourceList = null;
            SourceIndex = null;
            SourceName = null;
        }
    }

    private void DrawTooltip() {
        using var tooltip = ImRaii.Tooltip();

        var (color, message) = DragAction switch {
            DragAction.None => (ImGuiColors.DalamudGrey, "Drag "),
            DragAction.Reorder => (ImGuiColors.InfoForeground, "Reorder "),
            DragAction.Move => (ImGuiColors.WarningForeground, "Move "),
            DragAction.Copy => (ImGuiColors.SuccessForeground, "Copy "),
            _ => throw new ArgumentOutOfRangeException($"Unknown {nameof(DragAction)}: {DragAction}"),
        };

        ImGui.TextColored(color, message);
        ImGui.SameLine(0, 0);
        ImGui.Text(SourceName ?? "(Unknown)");

        if (DragAction == DragAction.Copy)
            ImGui.TextColored(ImGuiColors.DalamudGrey3, "(hold shift to move)");
    }

    public DragDisposable Drag(uint sourceId, List<T> list, int index) {
        var result = state.Drag(sourceId);
        if (result) {
            SourceList = list;
            SourceIndex = index;
        }
        return result;
    }

    public DropDisposable Drop(uint hoverId, List<T> destList, DragMask mask) {
        var result = state.Drop(hoverId);

        if (!result.Success || state.IsSource(hoverId) || SourceList is not { } sourceList)
            return result.Reject();

        if (ReferenceEquals(sourceList, destList)) {
            DragAction = DragAction.Reorder;
        } else {
            DragAction = ImGui.IsKeyDown(ImGuiKey.ModShift) ? DragAction.Move : DragAction.Copy;
        }

        if (!mask.Allows(DragAction))
            return result.Reject();

        return result.TryAccept();
    }

    public DragState GetDragState(uint id) => state.GetDragState(id);

    public ListOperations<T>? Element => SourceList is { } list && SourceIndex is { } index ? new ListOperations<T>(list, index) : null;
}

public class ListOperations<T>(List<T> list, int index) {
    public T Get() => list[index];

    public T Pop() {
        var val = list[index];
        list.RemoveAt(index);
        return val;
    }

    public void Swap(int toIndex) => list.Swap(index, toIndex);

    public void Swap(int toIndex, ref int trackedIndex) => list.Swap(index, toIndex, ref trackedIndex);
}

public enum DragAction : byte {
    None,
    Reorder,
    Move,
    Copy,
}

[Flags]
public enum DragMask : byte {
    None = 0,
    Reorder = 1 << 0,
    Move = 1 << 1,
    Copy = 1 << 2,

    Any = Reorder | Move | Copy,
    MoveOrCopy = Move | Copy,
}

public static class DragMaskExtensions {
    extension(DragMask mask) {
        public bool Allows(DragAction action) {
            var actionMask = action switch {
                DragAction.None => DragMask.None,
                DragAction.Reorder => DragMask.Reorder,
                DragAction.Move => DragMask.Move,
                DragAction.Copy => DragMask.Copy,
                _ => throw new ArgumentOutOfRangeException($"Unknown {nameof(DragAction)}: {action}"),
            };
            return mask.HasFlag(actionMask);
        }
    }
}
