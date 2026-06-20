using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;

namespace CurrencyWatchdog.Interface.Utility;

public sealed class DragDropState(string payloadId) {
    private readonly string payloadId = $"WATCHDOG_{payloadId}";
    private uint? SourceId { get; set; }
    private uint? HoverId { get; set; }

    private bool sawSourceThisFrame;
    private bool sawTargetThisFrame;

    public DragDisposable Drag(uint sourceId) {
        var inner = ImRaii.DragDropSource();
        if (inner) {
            sawSourceThisFrame = true;
            SourceId = sourceId;
            ImGui.SetDragDropPayload(payloadId, ReadOnlySpan<byte>.Empty);
        }

        return new DragDisposable(inner);
    }

    public DropBuilderDisposable Drop(uint hoverId) {
        return new DropBuilderDisposable(ImRaii.DragDropTarget(), this, hoverId);
    }

    public bool CheckDrop(uint hoverId) {
        sawTargetThisFrame = true;
        HoverId = hoverId;

        var payload = ImGui.AcceptDragDropPayload(payloadId);
        return !payload.IsNull;
    }

    public bool IsSource(uint id) => SourceId == id;

    public bool IsHovered(uint id) => HoverId == id;

    public DragState GetDragState(uint id) {
        return IsSource(id) ? DragState.Source : IsHovered(id) ? DragState.Target : DragState.None;
    }

    public bool CanDrop() {
        if (!sawTargetThisFrame) {
            HoverId = null;
            return false;
        }

        return true;
    }

    public bool IsActive() {
        var wasActive = sawSourceThisFrame;

        sawSourceThisFrame = false;
        sawTargetThisFrame = false;

        if (!ImGuiP.IsDragDropActive()) {
            SourceId = null;
            return false;
        }

        return wasActive;
    }
}

public ref struct DragDisposable(ImRaii.DragDropSourceDisposable inner) : IDisposable {
    private ImRaii.DragDropSourceDisposable inner = inner;

    public bool Success => inner.Success;

    public void Dispose() {
        inner.Dispose();
    }

    public static implicit operator bool(DragDisposable value) => value.inner.Success;
    public static bool operator true(DragDisposable i) => i.inner.Success;
    public static bool operator false(DragDisposable i) => !i.inner.Success;
    public static bool operator !(DragDisposable i) => !i.inner.Success;
    public static bool operator &(DragDisposable i, bool value) => i.inner.Success && value;
    public static bool operator |(DragDisposable i, bool value) => i.inner.Success || value;
}

public ref struct DropBuilderDisposable(ImRaii.DragDropTargetDisposable inner, DragDropState state, uint hoverId) : IDisposable {
    private ImRaii.DragDropTargetDisposable inner = inner;
    private bool alive = true;

    public bool Success => inner.Success;

    public DropDisposable Reject() {
        alive = false;
        return new DropDisposable(inner) {
            Hovered = false,
            Dropped = false,
        };
    }

    public DropDisposable TryAccept() {
        alive = false;
        return new DropDisposable(inner) {
            Hovered = true,
            Dropped = state.CheckDrop(hoverId),
        };
    }

    public void Dispose() {
        if (alive)
            inner.Dispose();
    }

    public static implicit operator bool(DropBuilderDisposable value) => value.inner.Success;
    public static bool operator true(DropBuilderDisposable i) => i.inner.Success;
    public static bool operator false(DropBuilderDisposable i) => !i.inner.Success;
    public static bool operator !(DropBuilderDisposable i) => !i.inner.Success;
    public static bool operator &(DropBuilderDisposable i, bool value) => i.inner.Success && value;
    public static bool operator |(DropBuilderDisposable i, bool value) => i.inner.Success || value;
}

public ref struct DropDisposable(ImRaii.DragDropTargetDisposable inner) : IDisposable {
    private ImRaii.DragDropTargetDisposable inner = inner;

    public bool Hovered { get; init; }
    public bool Dropped { get; init; }
    public void Dispose() => inner.Dispose();

    public static implicit operator bool(DropDisposable value) => value.Dropped;
    public static bool operator true(DropDisposable i) => i.Dropped;
    public static bool operator false(DropDisposable i) => !i.Dropped;
    public static bool operator !(DropDisposable i) => !i.Dropped;
    public static bool operator &(DropDisposable i, bool value) => i.Dropped && value;
    public static bool operator |(DropDisposable i, bool value) => i.Dropped || value;
}

public enum DragState {
    None,
    Source,
    Target,
}
