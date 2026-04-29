using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;

namespace CurrencyWatchdog.Interface.Utility;

public sealed class DragDropHelper(string payloadId) {
    private int? SourceIndex { get; set; }
    private int? HoverIndex { get; set; }

    private readonly string payloadId = $"WATCHDOG_{payloadId}";
    private bool sawSourceThisFrame;
    private bool sawTargetThisFrame;

    // ReSharper disable once UnusedMethodReturnValue.Global
    public bool EndFrame() {
        var dragEnded = SourceIndex.HasValue && !sawSourceThisFrame;

        if (!sawTargetThisFrame) {
            HoverIndex = null;
        }

        sawSourceThisFrame = false;
        sawTargetThisFrame = false;

        if (dragEnded) {
            SourceIndex = null;
        }

        return dragEnded;
    }

    public ImRaii.DragDropSourceDisposable Drag(int index) {
        var source = ImRaii.DragDropSource();
        if (source) {
            sawSourceThisFrame = true;
            SourceIndex = index;
            ImGui.SetDragDropPayload(payloadId, ReadOnlySpan<byte>.Empty);
        }
        return source;
    }

    public DragTargetEnd Drop(int index) {
        var inner = ImRaii.DragDropTarget();

        var sourceIndex = -1;
        var validDrop = false;

        if (inner) {
            sawTargetThisFrame = true;
            HoverIndex = index;

            var payload = ImGui.AcceptDragDropPayload(payloadId);
            if (!payload.IsNull && SourceIndex.HasValue) {
                var source = SourceIndex.Value;
                if (source != index) {
                    sourceIndex = source;
                    validDrop = true;
                } else {
                    SourceIndex = null;
                }
            }
        }

        return new DragTargetEnd(inner, validDrop, sourceIndex);
    }

    public bool IsSource(int index) => SourceIndex == index;

    public bool IsHovered(int index) => HoverIndex == index;

    public DragState GetDragState(int index) {
        return IsSource(index) ? DragState.Source : IsHovered(index) ? DragState.Target : DragState.None;
    }

    public enum DragState {
        None,
        Source,
        Target,
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
