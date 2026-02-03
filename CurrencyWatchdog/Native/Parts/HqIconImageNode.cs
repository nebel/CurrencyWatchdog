using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CurrencyWatchdog.Native.Parts;

public unsafe class HqIconImageNode : SimpleImageNode {

    public HqIconImageNode() {
        TextureSize = new Vector2(40, 40);
    }

    public void SetIcon(Icon icon) {
        IconSubFolder? subFolder = icon.IsHq ? IconSubFolder.HighQuality : null;

        // Why do it this way? For some reason, LoadIconTexture cannot load a HQ Cordial icon. I have no idea why.
        PartsList[0]->UldAsset->AtkTexture.LoadTexture(GetIconPath(icon.Id, subFolder));
    }

    private static string GetIconPath(uint iconId, IconSubFolder? iconSubFolder) {
        var textureManager = AtkStage.Instance()->AtkTextureResourceManager;
        var textureScale = textureManager->DefaultTextureScale;

        string? result = null;
        if (iconSubFolder is { } customSubFolder)
            result = GetIconPathIfExists(iconId, textureScale, customSubFolder);
        result ??= GetIconPathIfExists(iconId, textureScale, (IconSubFolder)textureManager->IconLanguage);
        result ??= GetIconPathIfExists(iconId, textureScale, IconSubFolder.None);
        return result ?? string.Empty;
    }

    private static string? GetIconPathIfExists(uint iconId, int textureScale, IconSubFolder iconSubFolder) {
        Span<byte> buffer = stackalloc byte[0x100];
        buffer.Clear();
        var bytePointer = (byte*)Unsafe.AsPointer(ref buffer[0]);
        AtkTexture.GetIconPath(bytePointer, iconId, textureScale, iconSubFolder);

        var pathResult = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(bytePointer));
        return Service.DataManager.FileExists(pathResult) ? pathResult : null;
    }
}
