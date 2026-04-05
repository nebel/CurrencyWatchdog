using CurrencyWatchdog.Configuration;
using KamiToolKit.Enums;
using KamiToolKit.Overlay.UiOverlay;
using System.Collections.Generic;
using System.Numerics;

namespace CurrencyWatchdog.Native;

public sealed class WatchdogContainerNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    public readonly List<CurrencyNode> Children = [];

    private int visibleChildren;

    public WatchdogContainerNode() {
        Position = new Vector2(400, 200);
        Size = new Vector2(20, 20);
    }

    protected override void OnUpdate() {
        IsVisible = visibleChildren != 0;
    }

    public void SetVisibleChildCount(int count) {
        while (Children.Count < count)
            CreateChild();

        for (var i = count; i < Children.Count; i++)
            Children[i].IsVisible = false;

        visibleChildren = count;
    }

    private void CreateChild() {
        var node = new CurrencyNode {
            Size = new Vector2(36, 36),
        };
        node.Configure(Plugin.Config.OverlayConfig);
        node.AttachNode(this);
        Children.Add(node);
    }

    public void Configure(OverlayConfig overlayConfig) {
        Position = overlayConfig.Position;
        foreach (var child in Children) {
            child.Configure(overlayConfig);
        }
    }
}
