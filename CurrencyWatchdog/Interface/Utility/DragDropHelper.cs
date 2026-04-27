using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;

namespace CurrencyWatchdog.Interface.Utility;

public sealed class DragDropHelper(string payloadId) {
    private int? SourceIndex { get; set; }
    private int? HoverIndex { get; set; }

    private readonly string payloadId = $"WATCHDOG_D_{payloadId}";
    private bool sawTargetThisFrame;

    public void EndFrame() {
        if (!sawTargetThisFrame) {
            HoverIndex = null;
        }

        sawTargetThisFrame = false;

        if (!ImGuiP.IsDragDropActive()) {
            SourceIndex = null;
        }
    }

    public ImRaii.DragDropSourceDisposable Drag(int index) {
        var source = ImRaii.DragDropSource();
        if (source) {
            SourceIndex = index;
            ImGui.SetDragDropPayload(payloadId, ReadOnlySpan<byte>.Empty);
        }
        return source;
    }

    public DragTargetEnd Drop(int index) {
        var inner = ImRaii.DragDropTarget();
        if (inner) {
            sawTargetThisFrame = true;
            HoverIndex = index;

            var payload = ImGui.AcceptDragDropPayload(payloadId);
            if (!payload.IsNull) {
                if (SourceIndex is { } sourceIndex) {
                    if (sourceIndex != index) {
                        return new DragTargetEnd(inner, true, sourceIndex);
                    }
                }
                SourceIndex = null;
                HoverIndex = null;
            }
        }

        return new DragTargetEnd(inner, false, -1);
    }

    private bool IsSource(int index) => SourceIndex == index;

    private bool IsHovered(int index) => HoverIndex == index && SourceIndex.HasValue;

    public DragState GetDragState(int index) {
        return IsSource(index) ? DragState.Source : IsHovered(index) ? DragState.Target : DragState.None;
    }

    public ref struct DragTargetEnd(ImRaii.DragDropTargetDisposable inner, bool success, int sourceIndex) : IDisposable {
        private ImRaii.DragDropTargetDisposable inner = inner;
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly bool Success = success;

        public int SourceIndex { get; } = sourceIndex;

        public void Dispose() => inner.Dispose();
        public static implicit operator bool(DragTargetEnd value) => value.Success;
        public static bool operator true(DragTargetEnd i) => i.Success;
        public static bool operator false(DragTargetEnd i) => !i.Success;
        public static bool operator !(DragTargetEnd i) => !i.Success;
        public static bool operator &(DragTargetEnd i, bool value) => i.Success && value;
        public static bool operator |(DragTargetEnd i, bool value) => i.Success || value;
    }
}