using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Interface.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;

namespace CurrencyWatchdog.Interface.Window.SettingsTabs;

public class MiscTab {
    public void Draw(Config config, ref bool changed) {
        using var child = ImRaii.Child("miscTabScrollChild");
        if (!child) return;

        ImGuiEx.ConfigTopHeader("Enable plugin");

        var enabled = config.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled)) {
            config.Enabled = enabled;
            changed = true;
        }

        ImGuiEx.ConfigHeader("Reset settings");

        using (ImRaii.Disabled(!(ImGui.GetIO().KeyShift && ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyAlt))) {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ExclamationTriangle, "Reset EVERYTHING!")) {
                Plugin.ConfigManager.FullReset();
            }
        }
        ImGuiEx.HoverTooltip("Reset EVERYTHING\n(hold shift+ctrl+alt to enable)");
    }
}
