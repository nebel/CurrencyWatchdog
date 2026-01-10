using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Interface.Utility;
using CurrencyWatchdog.Interface.Window.SettingsTabs;
using Dalamud.Bindings.ImGui;
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

    public override void OnClose() {
        base.OnClose();
        SubjectSelectorSlot.Invalidate();
        PresetSelectorSlot.Invalidate();
    }

    public override void Draw() {
        var config = Plugin.Config;
        var changed = false;

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

        SubjectSelectorSlot.EndFrame();
        PresetSelectorSlot.EndFrame();

        if (changed) {
            Plugin.ConfigManager.Save();
        }
    }
}
