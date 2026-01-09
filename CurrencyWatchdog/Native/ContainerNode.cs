using CurrencyWatchdog.Configuration;
using KamiToolKit.Classes;
using KamiToolKit.Overlay;
using System.Collections.Generic;
using System.Numerics;

namespace CurrencyWatchdog.Native;

public sealed class ContainerNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    public readonly List<CurrencyNode> Children = [];

    private int visibleChildren;

    public bool ForceHide {
        get;
        set {
            if (field != value) {
                field = value;
                UpdateVisibility();
            }
        }
    } = false;


    public ContainerNode() {
        Position = new Vector2(400, 200);
        Size = new Vector2(20, 20);
    }

    protected override void OnUpdate() {
        // Do nothing
    }

    private void UpdateVisibility() {
        if (ForceHide || visibleChildren == 0) {
            IsVisible = false;
        } else {
            IsVisible = true;
        }
    }

    public void SetVisibleChildCount(int count) {
        while (Children.Count < count)
            CreateChild();

        for (var i = count; i < Children.Count; i++)
            Children[i].IsVisible = false;

        visibleChildren = count;

        UpdateVisibility();
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
