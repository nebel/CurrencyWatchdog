using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Interface.Util;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using System;

namespace CurrencyWatchdog.Interface.Window.SettingsTabs;

public class OverlayTab {
    public void Draw(Config config, ref bool changed) {
        using var child = ImRaii.Child("overlayTabScrollChild");
        if (!child) return;

        var overlay = config.OverlayConfig;

        ImGuiEx.ConfigTopHeader("Overlay");

        var enabled = overlay.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled)) {
            overlay.Enabled = enabled;
            changed = true;
        }

        ImGuiEx.ConfigHeader("Visibility Settings");

        var hideInDuty = overlay.HideInDuty;
        if (ImGui.Checkbox("Hide in Duty", ref hideInDuty)) {
            overlay.HideInDuty = hideInDuty;
            changed = true;
        }

        ImGuiEx.ConfigHeader("Layout Settings");

        var position = overlay.Position;
        if (ImGui.DragFloat2("Position X/Y", ref position, 1f, -10000, 10000, flags: ImGuiSliderFlags.AlwaysClamp)) {
            overlay.Position = position;
            changed = true;
        }

        var layoutDirection = overlay.LayoutDirection;
        if (ImGuiEx.EnumCombo("Layout Direction", ref layoutDirection)) {
            overlay.LayoutDirection = layoutDirection;
            changed = true;
        }

        var iconSide = overlay.IconSide;
        if (ImGuiEx.EnumCombo("Icon Side", ref iconSide)) {
            overlay.IconSide = iconSide;
            changed = true;
        }

        ImGuiEx.ConfigHeader("Size Settings");

        var iconSize = overlay.IconSize;
        if (ImGui.DragFloat("Icon Size", ref iconSize, 1f, 4f, 128f, flags: ImGuiSliderFlags.AlwaysClamp)) {
            overlay.IconSize = iconSize;
            changed = true;
        }

        var panelSizing = overlay.PanelSizing;
        if (ImGuiEx.EnumCombo("Panel Width Mode", ref panelSizing)) {
            overlay.PanelSizing = panelSizing;
            changed = true;
        }

        if (panelSizing == PanelSizingType.Fixed) {
            var panelWidth = overlay.PanelWidth;
            if (ImGui.DragFloat("Panel Fixed Width", ref panelWidth, 1f, 0f, 500f, flags: ImGuiSliderFlags.AlwaysClamp)) {
                overlay.PanelWidth = panelWidth;
                changed = true;
            }
        }

        var scale = overlay.Scale;
        if (ImGui.DragFloat("Scale", ref scale, 0.01f, 0.1f, 30f, flags: ImGuiSliderFlags.AlwaysClamp)) {
            overlay.Scale = scale;
            changed = true;
        }
        ImGuiComponents.HelpMarker("Adjusting Icon Size and Label Font Size may give better results than changing the scale");

        ImGuiEx.ConfigHeader("Quantity Font Settings");

        var quantityFontSize = (int)overlay.QuantityFontSize;
        if (ImGui.SliderInt("Quantity Font Size", ref quantityFontSize, 6, 72)) {
            overlay.QuantityFontSize = (uint)Math.Clamp(quantityFontSize, 3, 200);
            changed = true;
        }

        var quantityFont = overlay.QuantityFont;
        if (ImGuiEx.EnumCombo("Quantity Font", ref quantityFont)) {
            overlay.QuantityFont = quantityFont;
            changed = true;
        }
        ImGuiComponents.HelpMarker("Letters and special characters may not display in some fonts.");

        var quantityFontOutline = overlay.QuantityFontOutline;
        if (ImGuiEx.EnumCombo("Quantity Font Outline", ref quantityFontOutline)) {
            overlay.QuantityFontOutline = quantityFontOutline;
            changed = true;
        }

        var quantityNodeOffset = overlay.QuantityNodeOffset;
        if (ImGui.DragFloat2("Quantity Text Offset X/Y", ref quantityNodeOffset, 1, -200, 200, flags: ImGuiSliderFlags.AlwaysClamp)) {
            overlay.QuantityNodeOffset = quantityNodeOffset;
            changed = true;
        }

        ImGuiEx.ConfigHeader("Label Font Settings");

        var labelFontSize = (int)overlay.LabelFontSize;
        if (ImGui.SliderInt("Label Font Size", ref labelFontSize, 6, 72)) {
            overlay.LabelFontSize = (uint)Math.Clamp(labelFontSize, 3, 200);
            changed = true;
        }

        var labelFont = overlay.LabelFont;
        if (ImGuiEx.EnumCombo("Label Font", ref labelFont)) {
            overlay.LabelFont = labelFont;
            changed = true;
        }
        ImGuiComponents.HelpMarker("Letters and special characters may not display in some fonts.");

        var labelFontOutline = overlay.LabelFontOutline;
        if (ImGuiEx.EnumCombo("Label Font Outline", ref labelFontOutline)) {
            overlay.LabelFontOutline = labelFontOutline;
            changed = true;
        }
        ImGuiEx.ConfigHeader("Spacing Settings");

        var panelGap = overlay.PanelGap;
        if (ImGui.DragFloat("Panel Gap", ref panelGap, 1f, -20, 100, flags: ImGuiSliderFlags.AlwaysClamp)) {
            overlay.PanelGap = panelGap;
            changed = true;
        }

        var panelPadding = overlay.PanelPadding;
        if (DrawSpacing("Panel Padding", ref panelPadding)) {
            overlay.PanelPadding = panelPadding;
            changed = true;
        }

        var labelPadding = overlay.LabelPadding;
        if (DrawSpacing("Label Padding", ref labelPadding)) {
            overlay.LabelPadding = labelPadding;
            changed = true;
        }

        ImGuiEx.ConfigHeader("Test");

        var showDummy = Plugin.Overlay.ShowDummy;
        if (ImGui.Checkbox("Show test panel", ref showDummy)) {
            Plugin.Overlay.ShowDummy = showDummy;
            changed = true;
        }

        ImGuiEx.ConfigHeader("Reset");

        using (ImRaii.Disabled(!ImGui.GetIO().KeyShift)) {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ExclamationTriangle, "Reset all overlay settings")) {
                Plugin.Config.OverlayConfig = new OverlayConfig();
                changed = true;
            }
        }
        ImGuiEx.HoverTooltip("Reset all overlay settings\n(hold shift to enable)");
    }

    private bool DrawSpacing(string label, ref Spacing spacing) {
        using var id = ImRaii.PushId(label);
        ImGui.Text(label);

        var left = spacing.Left;
        var right = spacing.Right;
        var top = spacing.Top;
        var bottom = spacing.Bottom;
        var changed = false;

        const int vSpeed = 1;
        const int vMin = -10;
        const int vMax = 50;

        if (ImGui.DragFloat("Left", ref left, vSpeed, vMin, vMax, flags: ImGuiSliderFlags.AlwaysClamp)) changed = true;
        if (ImGui.DragFloat("Right", ref right, vSpeed, vMin, vMax, flags: ImGuiSliderFlags.AlwaysClamp)) changed = true;
        if (ImGui.DragFloat("Top", ref top, vSpeed, vMin, vMax, flags: ImGuiSliderFlags.AlwaysClamp)) changed = true;
        if (ImGui.DragFloat("Bottom", ref bottom, vSpeed, vMin, vMax, flags: ImGuiSliderFlags.AlwaysClamp)) changed = true;

        if (changed)
            spacing = new Spacing(left, right, top, bottom);

        return changed;
    }

    private bool DrawSpacing(string label, ref HorizontalSpacing spacing) {
        using var id = ImRaii.PushId(label);
        ImGui.Text(label);

        var left = spacing.Left;
        var right = spacing.Right;
        var changed = false;

        const int vSpeed = 1;
        const int vMin = -10;
        const int vMax = 50;

        if (ImGui.DragFloat("Left", ref left, vSpeed, vMin, vMax, flags: ImGuiSliderFlags.AlwaysClamp)) changed = true;
        if (ImGui.DragFloat("Right", ref right, vSpeed, vMin, vMax, flags: ImGuiSliderFlags.AlwaysClamp)) changed = true;

        if (changed)
            spacing = new HorizontalSpacing(left, right);

        return changed;
    }
}
