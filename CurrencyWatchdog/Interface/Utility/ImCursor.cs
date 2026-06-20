using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace CurrencyWatchdog.Interface.Utility;

// Adapted from HaselDebug, which was adapted from ImSharp
// https://github.com/Ottermandias/ImSharp/blob/gc1.88/ImSharp/ImSharp/Cursor.cs

public static class ImCursor {
    private static bool Debug => false;

    public static Vector2 Position {
        get => ImGui.GetCursorPos();
        set => ImGui.SetCursorPos(value);
    }

    public static float X {
        get => ImGui.GetCursorPosX();
        set => ImGui.SetCursorPosX(value);
    }

    public static float Y {
        get => ImGui.GetCursorPosY();
        set => ImGui.SetCursorPosY(value);
    }

    public static Vector2 StartPosition => ImGui.GetCursorStartPos();

    public static Vector2 ScreenPosition {
        get => ImGui.GetCursorScreenPos();
        set => ImGui.SetCursorScreenPos(value);
    }

    public static unsafe void SameLineSpace() {
        var font = ImGui.GetFont();
        var glyph = font.FindGlyph(' ');
        var advanceX = glyph != null ? glyph->AdvanceX : font.FallbackGlyph.AdvanceX;
        var scale = ImGui.GetFontSize() / font.FontSize;
        ImGui.SameLine(0, advanceX * scale);
    }

    [SuppressMessage("ReSharper", "UseWithExpressionToCopyStruct")] // For consistency in the switch expression
    public static void ToNestedRect(Vector2 inner, Vector2 outer, ImAlign align = ImAlign.Center, Vector2? offset = null) {
        var delta = outer - inner;
        var relPos = align switch {
            ImAlign.TopLeft => Vector2.Zero,
            ImAlign.Top => new Vector2(delta.X / 2f, 0),
            ImAlign.TopRight => new Vector2(delta.X, 0),
            ImAlign.Left => new Vector2(0, delta.Y / 2f),
            ImAlign.Center => new Vector2(delta.X / 2f, delta.Y / 2f),
            ImAlign.Right => new Vector2(delta.X, delta.Y / 2f),
            ImAlign.BottomLeft => new Vector2(0, delta.Y),
            ImAlign.Bottom => new Vector2(delta.X / 2f, delta.Y),
            ImAlign.BottomRight => new Vector2(delta.X, delta.Y),
            _ => throw new ArgumentOutOfRangeException($"Unknown alignment: {align}"),
        };

        if (offset.HasValue)
            relPos += offset.Value;

        if (Debug) {
            var startPos = ImGui.GetCursorScreenPos();
            var innerPos = startPos + relPos;
            ImGui.GetWindowDrawList().AddRect(startPos, startPos + outer, ImGui.GetColorU32(ImGuiColors.DalamudRed));
            ImGui.GetWindowDrawList().AddRect(innerPos, innerPos + inner, ImGui.GetColorU32(ImGuiColors.DalamudOrange));
        }

        Position += relPos;
    }

    public static ExcursionDisposable Excursion() {
        return new ExcursionDisposable(ImGui.GetCursorPos());
    }

    public readonly ref struct ExcursionDisposable(Vector2 startPos) : IDisposable {
        public void Dispose() {
            ImGui.SetCursorPos(startPos);
        }
    }
}

public enum ImAlign {
    TopLeft,
    Top,
    TopRight,
    Left,
    Center,
    Right,
    BottomLeft,
    Bottom,
    BottomRight,
}
