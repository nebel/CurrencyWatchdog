using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;

namespace CurrencyWatchdog.Interface.Utility;

public sealed class CrossDragDropHelper<T>(string payloadId) {
    private List<T>? SourceList { get; set; }
    private int? SourceIndex { get; set; }
    private List<T>? HoverList { get; set; }
    private int? HoverIndex { get; set; }

    private bool sawTargetThisFrame;

    private readonly string payloadId = $"WATCHDOG_X_{payloadId}";

    public void EndFrame() {
        if (!sawTargetThisFrame) {
            HoverList = null;
            HoverIndex = null;
        }

        sawTargetThisFrame = false;

        if (!ImGuiP.IsDragDropActive()) {
            SourceList = null;
            SourceIndex = null;
        }
    }

    public ImRaii.DragDropSourceDisposable Drag(List<T> list, int index) {
        var source = ImRaii.DragDropSource();
        if (source) {
            SourceList = list;
            SourceIndex = index;
            ImGui.SetDragDropPayload(payloadId, ReadOnlySpan<byte>.Empty);
        }
        return source;
    }

    public DragTargetEnd Drop(List<T> list, int index) {
        var inner = ImRaii.DragDropTarget();
        if (inner) {
            sawTargetThisFrame = true;
            HoverList = list;
            HoverIndex = index;

            var payload = ImGui.AcceptDragDropPayload(payloadId);
            if (!payload.IsNull) {
                if (SourceIndex is { } sourceIndex && SourceList is { } sourceList) {
                    if (sourceIndex != index || !ReferenceEquals(sourceList, list)) {
                        return new DragTargetEnd(inner, true, sourceList, sourceIndex);
                    }
                }
                SourceList = null;
                SourceIndex = null;
                HoverList = null;
                HoverIndex = null;
            }
        }

        return new DragTargetEnd(inner, false, [], -1);

    }

    private bool IsSource(List<T> list, int index) => SourceList == list && SourceIndex == index;

    private bool IsHovered(List<T> list, int index) => HoverList == list && HoverIndex == index && SourceIndex.HasValue;

    public DragState GetDragState(List<T> list, int index) {
        return IsSource(list, index) ? DragState.Source : IsHovered(list, index) ? DragState.Target : DragState.None;
    }

    public ref struct DragTargetEnd(ImRaii.DragDropTargetDisposable inner, bool success, List<T> sourceList, int sourceIndex) : IDisposable {
        public bool Success { get; } = success;
        public List<T> SourceList { get; } = sourceList;
        public int SourceIndex { get; } = sourceIndex;

        private ImRaii.DragDropTargetDisposable inner = inner;

        public void Dispose() => inner.Dispose();

        public static bool operator true(DragTargetEnd i) => i.Success;
        public static bool operator false(DragTargetEnd i) => !i.Success;
        public static bool operator !(DragTargetEnd i) => !i.Success;
        public static bool operator &(DragTargetEnd i, bool value) => i.Success && value;
        public static bool operator |(DragTargetEnd i, bool value) => i.Success || value;
    }
}
