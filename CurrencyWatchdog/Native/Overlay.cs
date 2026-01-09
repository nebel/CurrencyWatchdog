using CurrencyWatchdog.Configuration;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using KamiToolKit.Extensions;
using KamiToolKit.Overlay;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CurrencyWatchdog.Native;

public sealed class Overlay : IDisposable {
    public bool ShowDummy { get; set; }

    private OverlayController? overlayController;
    private ContainerNode? container;

    public void FrameworkThreadInit() {
        if (MainThreadSafety.TryAssertMainThread()) return;

        Service.Log.Debug("Overlay::FrameworkThreadInit");
        overlayController = new OverlayController();
        container = new ContainerNode();

        overlayController.AddNode(container);
    }

    public void Dispose() {
        container?.Dispose();
        overlayController?.Dispose();
    }

    public void UpdateConfig(Config config) {
        if (MainThreadSafety.TryAssertMainThread()) return;
        if (container is null) {
            Service.Log.Warning("Got config update event, but the node container is not ready yet. Skipping.");
            return;
        }

        container.Configure(config.OverlayConfig);
    }

    public void UpdateNodes(List<Alert> alerts) {
        if (MainThreadSafety.TryAssertMainThread()) return;
        if (container is null) {
            Service.Log.Warning("Got node update event, but the node container is not ready yet. Skipping.");
            return;
        }

        if (ShowDummy) {
            alerts = new List<Alert>(alerts);
            alerts.Insert(0, Alert.Dummy);
        }

        container.SetVisibleChildCount(alerts.Count);

        if (alerts.Count == 0)
            return;

        var overlayConfig = Plugin.Config.OverlayConfig;
        var direction = overlayConfig.LayoutDirection;
        var padding = overlayConfig.PanelPadding;
        var gap = overlayConfig.PanelGap;

        var currentPosition = new Vector2();
        for (var i = 0; i < alerts.Count; i++) {
            var element = alerts[i];
            var node = container.Children[i];
            node.Apply(overlayConfig, PanelPayload.From(element));

            if (direction == LayoutAnchor.Left)
                currentPosition -= new Vector2(node.ContentSize.X + padding.Horizontal + gap, 0);

            node.Position = currentPosition;

            if (direction is LayoutAnchor.UpLeft or LayoutAnchor.DownLeft)
                node.Position -= new Vector2(node.ContentSize.X + padding.Horizontal + gap, 0);

            currentPosition += direction switch {
                LayoutAnchor.UpLeft or LayoutAnchor.UpRight => new Vector2(0, -node.ContentSize.Y - padding.Vertical - gap),
                LayoutAnchor.DownLeft or LayoutAnchor.DownRight => new Vector2(0, node.ContentSize.Y + padding.Vertical + gap),
                LayoutAnchor.Right => new Vector2(node.ContentSize.X + padding.Horizontal + gap, 0),
                _ => Vector2.Zero,
            };
        }

        if (Math.Abs(container.ScaleX - overlayConfig.Scale) > 0.001)
            container.Scale = new Vector2(overlayConfig.Scale, overlayConfig.Scale);
    }

    public void ClearNodes() {
        if (MainThreadSafety.TryAssertMainThread()) return;
        if (container is null) {
            Service.Log.Warning("Got node update event, but the node container is not ready yet. Skipping.");
            return;
        }

        container.SetVisibleChildCount(0);
    }
}
