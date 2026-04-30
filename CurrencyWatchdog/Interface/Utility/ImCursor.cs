using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using System;
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

    public static void ToNestedRect(Vector2 inner, Vector2 outer, Vector2? offset = null) {
        var relPos = (outer / 2) - (inner / 2);
        if (offset.HasValue) {
            relPos += offset.Value;
        }
        if (Debug) {
            var startPos = ImGui.GetCursorScreenPos();
            var innerPos = startPos + relPos;
            ImGui.GetWindowDrawList().AddRect(startPos, startPos + outer, ImGui.GetColorU32(ImGuiColors.DalamudRed));
            ImGui.GetWindowDrawList().AddRect(innerPos, innerPos + inner, ImGui.GetColorU32(ImGuiColors.ParsedGreen));
        }
        Position += relPos;
    }

    public static void ToNestedRectY(Vector2 inner, Vector2 outer, Vector2? offset = null) {
        var relPos = ((outer / 2) - (inner / 2)) with { X = 0 };
        if (offset.HasValue) {
            relPos += offset.Value;
        }
        if (Debug) {
            var startPos = ImGui.GetCursorScreenPos();
            var innerPos = startPos + relPos;
            ImGui.GetWindowDrawList().AddRect(startPos, startPos + outer, ImGui.GetColorU32(ImGuiColors.DalamudRed));
            ImGui.GetWindowDrawList().AddRect(innerPos, innerPos + inner, ImGui.GetColorU32(ImGuiColors.DalamudOrange));
        }
        Position += relPos;
    }

    public static ExcursionEndObject Excursion() {
        return new ExcursionEndObject(ImGui.GetCursorPos());
    }

    public readonly ref struct ExcursionEndObject(Vector2 startPos) : IDisposable {
        public void Dispose() {
            ImGui.SetCursorPos(startPos);
        }
    }
}
