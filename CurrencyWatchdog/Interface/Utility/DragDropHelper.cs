using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;

namespace CurrencyWatchdog.Interface.Utility;

public sealed class DragDropHelper<T>(string payloadId) {
    private uint? SourceId { get; set; }
    private List<T>? SourceList { get; set; }
    private int? SourceIndex { get; set; }
    public string? SourceName { get; private set; }
    public uint? HoverId { get; private set; }

    private bool sawTargetThisFrame;
    private bool sawSourceThisFrame;
    private DragAction dragAction = DragAction.None;

    private readonly string payloadId = $"WATCHDOG_M_{payloadId}";

    public void EndFrame() {
        if (!sawTargetThisFrame) {
            HoverId = null;
            dragAction = DragAction.None;
        }

        if (ImGuiP.IsDragDropActive() && sawSourceThisFrame) {
            using (ImRaii.Tooltip()) {
                var sourceName = SourceName ?? "(Unknown)";
                switch (dragAction) {
                    case DragAction.None:
                        ImGui.TextColored(ImGuiColors.DalamudGrey, $"Drag");
                        break;
                    case DragAction.Reorder:
                        ImGui.TextColored(ImGuiColors.InfoForeground, $"Reorder");
                        break;
                    case DragAction.Move:
                        ImGui.TextColored(ImGuiColors.WarningForeground, $"Move");
                        break;
                    case DragAction.Copy:
                        ImGui.TextColored(ImGuiColors.SuccessForeground, $"Copy");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unknown ${nameof(DragAction)}: {dragAction}");
                }
                ImCursor.SameLineSpace();
                ImGui.Text($"{sourceName}");
                if (dragAction == DragAction.Copy)
                    ImGui.TextColored(ImGuiColors.DalamudGrey3, $"(hold shift to move)");
            }
        }

        sawSourceThisFrame = false;
        sawTargetThisFrame = false;

        if (!ImGuiP.IsDragDropActive()) {
            SourceId = null;
            SourceList = null;
            SourceIndex = null;
            SourceName = null;
        }
    }

    public DragEnd Drag(uint sourceId, List<T> list, int index) {
        var source = ImRaii.DragDropSource();
        if (source) {
            sawSourceThisFrame = true;
            SourceId = sourceId;
            SourceList = list;
            SourceIndex = index;
            ImGui.SetDragDropPayload(payloadId, ReadOnlySpan<byte>.Empty);
        }
        return new DragEnd(source, this);
    }

    public DropEnd Drop(uint hoverId, List<T> destList, DragMask mask) {
        var inner = ImRaii.DragDropTarget();
        if (inner) {
            if (SourceIndex is { } sourceIndex && SourceList is { } sourceList) {
                var sameList = ReferenceEquals(sourceList, destList);
                if (sameList) {
                    dragAction = DragAction.Reorder;
                } else {
                    dragAction = ImGui.IsKeyDown(ImGuiKey.ModShift) ? DragAction.Move : DragAction.Copy;
                }

                if (IsSource(hoverId) || !mask.Allows(dragAction)) {
                    return new DropEnd(inner, false, false, dragAction, [], -1);
                }

                sawTargetThisFrame = true;
                HoverId = hoverId;

                var payload = ImGui.AcceptDragDropPayload(payloadId);
                return new DropEnd(inner, true, !payload.IsNull, dragAction, sourceList, sourceIndex);
            }
        }

        return new DropEnd(inner, false, false, dragAction, [], -1);
    }

    private bool IsSource(uint sourceId) => SourceId == sourceId;

    private bool IsHovered(uint hoverId) => HoverId == hoverId && SourceIndex.HasValue;

    public DragState GetDragState(uint id) {
        return IsSource(id) ? DragState.Source : IsHovered(id) ? DragState.Target : DragState.None;
    }

    public ref struct DragEnd(ImRaii.DragDropSourceDisposable inner, DragDropHelper<T> parent) : IDisposable {
        private ImRaii.DragDropSourceDisposable inner = inner;

        public void SetSourceName(string name) => parent.SourceName = name;

        public string SourceName { set => parent.SourceName = value; }

        public void Dispose() {
            inner.Dispose();
        }

        public static implicit operator bool(DragEnd value) => value.inner.Success;
        public static bool operator true(DragEnd i) => i.inner.Success;
        public static bool operator false(DragEnd i) => !i.inner.Success;
        public static bool operator !(DragEnd i) => !i.inner.Success;
        public static bool operator &(DragEnd i, bool value) => i.inner.Success && value;
        public static bool operator |(DragEnd i, bool value) => i.inner.Success || value;
    }

    public ref struct DropEnd(ImRaii.DragDropTargetDisposable inner, bool hovered, bool dropped, DragAction action, List<T> sourceList, int sourceIndex) : IDisposable {
        private ImRaii.DragDropTargetDisposable inner = inner;

        public bool Hovered { get; } = hovered;
        public bool Dropped { get; } = dropped;
        public DragAction Action { get; } = action;
        public List<T> SourceList { get; } = sourceList;
        public int SourceIndex { get; } = sourceIndex;

        public T Target => SourceList[SourceIndex];

        public T PopTarget() {
            var target = SourceList[SourceIndex];
            SourceList.RemoveAt(SourceIndex);
            return target;
        }

        public void Dispose() => inner.Dispose();

        public static implicit operator bool(DropEnd value) => value.Dropped;
        public static bool operator true(DropEnd i) => i.Dropped;
        public static bool operator false(DropEnd i) => !i.Dropped;
        public static bool operator !(DropEnd i) => !i.Dropped;
        public static bool operator &(DropEnd i, bool value) => i.Dropped && value;
        public static bool operator |(DropEnd i, bool value) => i.Dropped || value;
    }
}
