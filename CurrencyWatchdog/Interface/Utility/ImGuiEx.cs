using CurrencyWatchdog.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Linq;
using System.Numerics;

namespace CurrencyWatchdog.Interface.Utility;

public static class ImGuiEx {
    public static bool NullableInputInt(string label, int defaultValue, ref int? value) {
        using var id = ImRaii.PushId($"nullableInputInt:{label}");

        var hasValue = value is not null;
        var localValue = value ?? defaultValue;

        bool valueChanged;
        using (ImRaii.Disabled(!hasValue)) {
            valueChanged = ImGui.InputInt($"##value", ref localValue, 1, 1);
        }

        ImGui.SameLine();
        var hasValueChanged = ImGui.Checkbox($"{label}##check", ref hasValue);

        if (!valueChanged && !hasValueChanged)
            return false;

        value = hasValue ? localValue : null;
        return true;
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
        if (values.Length != 0 && values[0].GetDisplayOrder() is not null) {
            Array.Sort(values, (a, b) => a.GetDisplayOrder()!.Value.CompareTo(b.GetDisplayOrder()!.Value));
        }

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

    public static void TemplateHelp() {
        ImGuiComponents.HelpMarker("See \"Template strings\" in the Help tab.");
    }

    public static Vector4 GetFadedColor(ImGuiCol col, float multiplier) {
        return GetFadedColor(ImGui.GetStyle().Colors[(int)col], multiplier);
    }

    public static Vector4 GetFadedColor(Vector4 color, float multiplier) {
        var w = color.W * multiplier;
        return color with { W = w };
    }

    public static void DrawRoundedBackground(float rowHeight, Vector4 backgroundColor, Vector4 borderColor, float? dividerOffset = null) {
        var topLeft = ImGui.GetCursorScreenPos();
        var bottomRight = topLeft + ImGuiHelpers.ScaledVector2(ImGui.GetContentRegionAvail().X, rowHeight);

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(topLeft, bottomRight, ImGui.GetColorU32(backgroundColor), 3f);
        drawList.AddRect(topLeft, bottomRight, ImGui.GetColorU32(borderColor), 3f, ImDrawFlags.None, 1f);

        if (dividerOffset is { } offset) {
            var lineTop = topLeft + new Vector2(offset, 0);
            var lineBottom = lineTop + new Vector2(0, rowHeight);
            drawList.AddLine(lineTop, lineBottom, ImGui.GetColorU32(borderColor), 1f);
        }
    }
}
