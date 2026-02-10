using KamiToolKit.Nodes;
using System.Numerics;

namespace CurrencyWatchdog.Native.Parts;

public unsafe class HqIconImageNode : SimpleImageNode {
    private Icon currentIcon = new(0);

    public HqIconImageNode() {
        TextureSize = new Vector2(40, 40);
        SetIcon(new Icon(59234)); // Ensure some default icon is set
    }

    public void SetIcon(Icon icon) {
        if (currentIcon == icon) return;
        currentIcon = icon;

        var iconId = icon.IsHq ? icon.Id + 1_000_000 : icon.Id;
        PartsList[0]->UldAsset->AtkTexture.LoadIconTexture(iconId);
    }
}
