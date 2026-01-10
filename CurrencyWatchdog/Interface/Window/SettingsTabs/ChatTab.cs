using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Interface.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using System;

namespace CurrencyWatchdog.Interface.Window.SettingsTabs;

public class ChatTab {
    public void Draw(Config config, ref bool changed) {
        using var child = ImRaii.Child("chatTabScrollChild");
        if (!child) return;

        var chat = config.ChatConfig;

        ImGuiEx.ConfigTopHeader("Chat Alerts");

        var enabled = chat.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled)) {
            chat.Enabled = enabled;
            changed = true;
        }

        ImGuiEx.ConfigHeader("Actions");

        var loginAction = chat.LoginAction;
        if (ImGuiEx.EnumCombo("When logging in...", ref loginAction)) {
            chat.LoginAction = loginAction;
            changed = true;
        }

        var zoneAction = chat.ZoneAction;
        if (ImGuiEx.EnumCombo("When changing zones...", ref zoneAction)) {
            chat.ZoneAction = zoneAction;
            changed = true;
        }

        var alertUpdateAction = chat.AlertUpdateAction;
        if (ImGuiEx.EnumCombo("When active alerts are updated...", ref alertUpdateAction)) {
            chat.AlertUpdateAction = alertUpdateAction;
            changed = true;
        }

        ImGuiEx.ConfigHeader("Sounds");

        var playSound = chat.PlaySound;
        if (ImGui.Checkbox("Play Sound", ref playSound)) {
            chat.PlaySound = playSound;
            changed = true;
        }

        var effectId = (int)chat.SoundEffectId;
        if (ImGui.SliderInt("Sound Effect", ref effectId, 1, 16)) {
            chat.SoundEffectId = (uint)Math.Clamp(effectId, 1, 16);
            if (playSound)
                Chat.PlaySound((uint)effectId);
            changed = true;
        }

        using (ImRaii.PushId("prefix")) {
            ImGuiEx.ConfigHeader("Message Prefix");

            var prefix = chat.Prefix;
            if (ImGui.InputText("Text", ref prefix)) {
                chat.Prefix = prefix;
                changed = true;
            }

            var prefixColor = chat.PrefixColor;
            if (ImGui.ColorEdit4("Prefix Color", ref prefixColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar)) {
                chat.PrefixColor = prefixColor;
                changed = true;
            }

            var prefixOutlineColor = chat.PrefixOutlineColor;
            if (ImGui.ColorEdit4("Prefix Outline Color", ref prefixOutlineColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar)) {
                chat.PrefixOutlineColor = prefixOutlineColor;
                changed = true;
            }
        }

        using (ImRaii.PushId("message")) {
            ImGuiEx.ConfigHeader("Message");

            var message = chat.MessageTemplate;
            if (ImGui.InputText("Template", ref message)) {
                chat.MessageTemplate = message;
                changed = true;
            }
            ImGuiEx.TemplateHelp();

            var messageColor = chat.MessageColor;
            if (ImGui.ColorEdit4("Color", ref messageColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar)) {
                chat.MessageColor = messageColor;
                changed = true;
            }

            var messageOutlineColor = chat.MessageOutlineColor;
            if (ImGui.ColorEdit4("Outline Color", ref messageOutlineColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar)) {
                chat.MessageOutlineColor = messageOutlineColor;
                changed = true;
            }
        }

        using (ImRaii.PushId("suffix")) {
            ImGuiEx.ConfigHeader("Message Suffix");

            var suffix = chat.SuffixTemplate;
            if (ImGui.InputText("Template", ref suffix)) {
                chat.SuffixTemplate = suffix;
                changed = true;
            }
            ImGuiEx.TemplateHelp();

            var suffixColor = chat.SuffixColor;
            if (ImGui.ColorEdit4("Color", ref suffixColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar)) {
                chat.SuffixColor = suffixColor;
                changed = true;
            }

            var suffixOutlineColor = chat.SuffixOutlineColor;
            if (ImGui.ColorEdit4("Outline Color", ref suffixOutlineColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar)) {
                chat.SuffixOutlineColor = suffixOutlineColor;
                changed = true;
            }
        }

        ImGuiEx.ConfigHeader("Test");

        if (ImGui.Button("Send test message")) {
            Chat.SendChatAlerts([Alert.Dummy]);
        }
        ImGui.SameLine();
        if (ImGui.Button("Re-send message for active rules")) {
            Plugin.AlertUpdater.ResendActiveChatAlerts();
        }

        ImGuiEx.ConfigHeader("Reset");

        using (ImRaii.Disabled(!ImGui.GetIO().KeyShift)) {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ExclamationTriangle, "Reset all chat settings")) {
                Plugin.Config.ChatConfig = new ChatConfig();
                changed = true;
            }
        }
        ImGuiEx.HoverTooltip("Reset all chat settings\n(hold shift to enable)");
    }
}
