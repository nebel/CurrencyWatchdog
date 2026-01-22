using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Interface.Utility;
using CurrencyWatchdog.Interface.Window.SettingsTabs;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CurrencyWatchdog.Interface.Window;

public class ConfigWindow : Dalamud.Interface.Windowing.Window {
    public readonly LeasedImmediateSlot<Guid, List<Subject>> SubjectSelectorSlot;
    public readonly LeasedImmediateSlot<Guid, List<Burden>> PresetSelectorSlot;

    private readonly BurdensTab burdensTab;
    private readonly ChatTab chatTab;
    private readonly OverlayTab overlayTab;
    private readonly PanelTab panelTab;
    private readonly MiscTab miscTab;
    private readonly HelpTab helpTab;

    private Config? backupConfig;

    public ConfigWindow() : base("Currency Watchdog Settings", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse) {
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(800, 600) };

        SubjectSelectorSlot = new LeasedImmediateSlot<Guid, List<Subject>>();
        SubjectSelectorSlot.Invalidated += () => { Plugin.WindowManager.SubjectSelectorWindow.IsOpen = false; };

        PresetSelectorSlot = new LeasedImmediateSlot<Guid, List<Burden>>();
        PresetSelectorSlot.Invalidated += () => { Plugin.WindowManager.PresetSelectorWindow.IsOpen = false; };

        burdensTab = new BurdensTab(this);
        chatTab = new ChatTab();
        overlayTab = new OverlayTab();
        panelTab = new PanelTab();
        miscTab = new MiscTab();
        helpTab = new HelpTab();
    }

    public override void OnOpen() {
        base.OnOpen();
        backupConfig = Plugin.Config.Clone();
    }

    public override void OnClose() {
        base.OnClose();
        backupConfig = null;
        SubjectSelectorSlot.Invalidate();
        PresetSelectorSlot.Invalidate();
    }

    public override void Draw() {
        var config = Plugin.Config;
        var changed = false;

        var startCursor = ImGui.GetCursorPos();

        using (var tabBar = ImRaii.TabBar("ConfigTabs")) {
            if (tabBar) {
                using (var tab = ImRaii.TabItem("Burdens")) {
                    if (tab) {
                        burdensTab.Draw(config, ref changed);
                    }
                }

                using (var tab = ImRaii.TabItem("Overlay")) {
                    if (tab) {
                        overlayTab.Draw(config, ref changed);
                    }
                }

                using (var tab = ImRaii.TabItem("Overlay Panels")) {
                    if (tab) {
                        panelTab.Draw(config, ref changed);
                    }
                }

                using (var tab = ImRaii.TabItem("Chat Alerts")) {
                    if (tab) {
                        chatTab.Draw(config, ref changed);
                    }
                }

                using (var tab = ImRaii.TabItem("Misc")) {
                    if (tab) {
                        miscTab.Draw(config, ref changed);
                    }
                }

                using (var tab = ImRaii.TabItem("Help")) {
                    if (tab) {
                        helpTab.Draw();
                    }
                }
            }
        }

        DrawRevertButton(startCursor);

        SubjectSelectorSlot.EndFrame();
        PresetSelectorSlot.EndFrame();

        if (changed) {
            Plugin.ConfigManager.Save();
        }
    }
    private void DrawRevertButton(Vector2 startCursor) {
        var buttonWidth = ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.Undo, "Revert");
        ImGui.SetCursorPos(startCursor + new Vector2(ImGui.GetContentRegionAvail().X - buttonWidth, -(ImGui.GetStyle().WindowPadding.Y / 2)));
        using (ImRaii.Disabled(!ImGui.GetIO().KeyShift)) {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Undo, "Revert")) {
                if (backupConfig != null) {
                    Plugin.ConfigManager.LoadObject(backupConfig.Clone());
                }
            }
        }
        ImGuiEx.HoverTooltip("Revert all changes made since opening the Settings window\n(hold shift to enable)");
    }
}
