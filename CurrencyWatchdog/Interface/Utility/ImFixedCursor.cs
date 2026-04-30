using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace CurrencyWatchdog.Interface.Utility;

public class ImFixedCursor {
    private Vector2 current;

    public ImFixedCursor() {
        current = ImGui.GetCursorPos();
    }

    public ImFixedCursor(Vector2 startPosition) {
        current = startPosition;
        ImGui.SetCursorPos(startPosition);
    }

    public Vector2 Position {
        get => current;
        set => ImGui.SetCursorPos(current = value);
    }

    public float X {
        get => current.X;
        set => ImGui.SetCursorPos(current = current with { X = value });
    }

    public float Y {
        get => current.Y;
        set => ImGui.SetCursorPos(current = current with { Y = value });
    }
}
