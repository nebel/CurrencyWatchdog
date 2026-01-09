using CurrencyWatchdog.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CurrencyWatchdog.Interface.Util;

public static class ImGuiEx {
    public static IDisposable CursorExcursion() {
        return new ExcursionEndObject(ImGui.GetCursorPos());
    }

    private sealed class ExcursionEndObject(Vector2 startPos) : IDisposable {
        public void Dispose() {
            ImGui.SetCursorPos(startPos);
        }
    }

    public static bool NullableInputText(string label, string defaultText, ref string? text, int maxLength = 200) {
        using var id = ImRaii.PushId($"nullableInputText:{label}");

        var hasValue = text is not null;
        var localText = text ?? defaultText;

        bool textChanged;
        using (ImRaii.Disabled(!hasValue)) {
            textChanged = ImGui.InputText($"##text", ref localText, maxLength);
        }

        ImGui.SameLine();
        var hasValueChanged = ImGui.Checkbox($"{label}##check", ref hasValue);

        if (!textChanged && !hasValueChanged)
            return false;

        text = hasValue ? localText : null;
        return true;
    }

    public static bool NullableColorEdit4(string label, Vector4 defaultColor, ref Vector4? color) {
        using var id = ImRaii.PushId($"nullableColorEdit4:{label}");

        var hasValue = color.HasValue;
        var localColor = color ?? defaultColor;

        bool colorChanged;
        using (ImRaii.Disabled(!hasValue)) {
            colorChanged = ImGui.ColorEdit4("##color", ref localColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar);
        }

        ImGui.SameLine();
        var hasValueChanged = ImGui.Checkbox($"{label}##check", ref hasValue);

        if (!colorChanged && !hasValueChanged)
            return false;

        color = hasValue ? localColor : null;
        return true;
    }

    public static bool EnumCombo<T>(string label, ref T value) where T : struct, Enum {
        var values = Enum.GetValues<T>();
        var names = values.Select(e => e.GetDisplayName()).ToArray();
        var index = Array.IndexOf(values, value);

        if (ImGui.Combo(label, ref index, names, values.Length)) {
            value = values[index];
            return true;
        }

        return false;
    }

    public static void HoverTooltip(string text) {
        if (text == "") return;
        using (ImRaii.DefaultStyle()) {
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                ImGui.SetTooltip(text);
            }
        }
    }

    public static void CenteredText(string text) {
        ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X / 2f - ImGui.CalcTextSize(text).X / 2f);
        ImGui.TextUnformatted(text);
    }

    public static void SpacedSeparator() {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    public static void ConfigTopHeader(string text) {
        ImGui.Text(text);
        ImGui.Separator();
        ImGui.Spacing();
    }

    public static void ConfigHeader(string text) {
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Text(text);
        ImGui.Separator();
        ImGui.Spacing();
    }

    public static void FormatHelp() {
        ImGuiComponents.HelpMarker("See \"Format strings\" in the Help tab.");
    }
}

public sealed class EnumDrawData<T> : IEnumerable<EnumDrawData<T>.Item> where T : struct, Enum {
    public readonly T[] Values;
    public readonly string[] Names;
    public readonly float Width;

    public static EnumDrawData<T> Calculate() => new();

    private EnumDrawData() {
        Values = Enum.GetValues<T>();
        Names = new string[Values.Length];

        var maxTextWidth = 0f;
        for (var i = 0; i < Values.Length; i++) {
            var name = Values[i].GetDisplayName();

            var textWidth = ImGui.CalcTextSize(name).X;
            if (textWidth > maxTextWidth)
                maxTextWidth = textWidth;

            Names[i] = name;
        }

        Width = maxTextWidth + (ImGui.GetStyle().FramePadding.X * 2) + ImGui.GetFrameHeight();
    }

    public IEnumerator<Item> GetEnumerator() {
        for (var i = 0; i < Values.Length; i++)
            yield return new Item(Values[i], Names[i]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public readonly struct Item {
        public readonly T Value;
        public readonly string DrawName;

        internal Item(T type, string drawName) {
            Value = type;
            DrawName = drawName;
        }
    }
}
