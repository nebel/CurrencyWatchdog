using System;

namespace CurrencyWatchdog.Interface.Utility;

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
