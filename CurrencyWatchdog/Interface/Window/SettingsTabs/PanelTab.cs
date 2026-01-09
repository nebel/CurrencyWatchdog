using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Interface.Util;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;

namespace CurrencyWatchdog.Interface.Window.SettingsTabs;

public class PanelTab {
    public void Draw(Config config, ref bool changed) {
        using var child = ImRaii.Child("panelTabScrollChild");
        if (!child) return;

        var panel = config.PanelConfig;

        using (ImRaii.PushId("quantity")) {
            ImGuiEx.ConfigTopHeader("Quantity Display");

            var quantityFormat = panel.QuantityFormat;
            if (ImGui.InputText("Format", ref quantityFormat)) {
                panel.QuantityFormat = quantityFormat;
                changed = true;
            }
            ImGuiEx.FormatHelp();

            var quantityColor = panel.QuantityColor;
            if (ImGui.ColorEdit4("Color", ref quantityColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar)) {
                panel.QuantityColor = quantityColor;
                changed = true;
            }

            var quantityOutlineColor = panel.QuantityOutlineColor;
            if (ImGui.ColorEdit4("Outline Color", ref quantityOutlineColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar)) {
                panel.QuantityOutlineColor = quantityOutlineColor;
                changed = true;
            }
        }

        using (ImRaii.PushId("label")) {
            ImGuiEx.ConfigHeader("Label Display");

            var labelFormat = panel.LabelFormat;
            if (ImGui.InputText("Format", ref labelFormat)) {
                panel.LabelFormat = labelFormat;
                changed = true;
            }
            ImGuiEx.FormatHelp();

            var labelColor = panel.LabelColor;
            if (ImGui.ColorEdit4("Color", ref labelColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar)) {
                panel.LabelColor = labelColor;
                changed = true;
            }

            var labelOutlineColor = panel.LabelOutlineColor;
            if (ImGui.ColorEdit4("Outline Color", ref labelOutlineColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar)) {
                panel.LabelOutlineColor = labelOutlineColor;
                changed = true;
            }
        }

        using (ImRaii.PushId("Backdrop")) {
            ImGuiEx.ConfigHeader("Backdrop");

            var backdropColor = panel.BackdropColor;
            if (ImGui.ColorEdit4("Backdrop Color", ref backdropColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar)) {
                panel.BackdropColor = backdropColor;
                changed = true;
            }
        }

        ImGuiEx.ConfigHeader("Test");

        var showDummy = Plugin.Overlay.ShowDummy;
        if (ImGui.Checkbox("Show test panel", ref showDummy)) {
            Plugin.Overlay.ShowDummy = showDummy;
            changed = true;
        }

        ImGuiEx.ConfigHeader("Reset");

        using (ImRaii.Disabled(!ImGui.GetIO().KeyShift)) {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ExclamationTriangle, "Reset all overlay panel settings")) {
                Plugin.Config.PanelConfig = new PanelConfig();
                changed = true;
            }
        }
        ImGuiEx.HoverTooltip("Reset all overlay panel settings\n(hold shift to enable)");
    }
}
