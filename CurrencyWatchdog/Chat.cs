using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Collections.Generic;

namespace CurrencyWatchdog;

public static class Chat {
    public static void SendChatAlerts(List<Alert> alert) {
        foreach (var a in alert) {
            SendChatAlert(a);
        }

        if (alert.Count > 0 && Plugin.Config.ChatConfig is { PlaySound: true, SoundEffectId: var effectId and >= 1 and <= 16 }) {
            PlaySound(effectId);
        }
    }

    public static void PlaySound(uint effectId) {
        UIGlobals.PlayChatSoundEffect(effectId);
    }

    private static void SendChatAlert(Alert alert) {
        using var rented = new RentedSeStringBuilder();

        var global = Plugin.Config.ChatConfig;
        var payload = ChatPayload.From(alert);
        var s = rented
            .Builder
            .PushColorRgba(global.PrefixColor).PushEdgeColorRgba(global.PrefixOutlineColor)
            .Append(global.Prefix)
            .PopEdgeColor().PopColor()
            .AppendIcon((uint)BitmapFontIcon.Warning)
            .PushColorRgba(payload.MessageColor).PushEdgeColorRgba(payload.MessageOutlineColor)
            .Append(payload.Message)
            .PopEdgeColor().PopColor()
            .PushColorRgba(payload.SuffixColor).PushEdgeColorRgba(payload.SuffixOutlineColor)
            .Append(payload.Suffix)
            .PopEdgeColor().PopColor();

        Service.ChatGui.Print(s.GetViewAsSpan());
    }
}
