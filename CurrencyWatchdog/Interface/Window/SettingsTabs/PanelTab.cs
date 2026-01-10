using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Interface.Utility;
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

            var quantityTemplate = panel.QuantityTemplate;
            if (ImGui.InputText("Template", ref quantityTemplate)) {
                panel.QuantityTemplate = quantityTemplate;
                changed = true;
            }
            ImGuiEx.TemplateHelp();

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

            var labelTemplate = panel.LabelTemplate;
            if (ImGui.InputText("Template", ref labelTemplate)) {
                panel.LabelTemplate = labelTemplate;
                changed = true;
            }
            ImGuiEx.TemplateHelp();

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
