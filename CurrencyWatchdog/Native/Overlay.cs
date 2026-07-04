using CurrencyWatchdog.Configuration;
using Dalamud.Utility;
using KamiToolKit.UiOverlay;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CurrencyWatchdog.Native;

public sealed class Overlay : IDisposable {
    public bool ShowDummy { get; set; }

    private OverlayController? overlayController;
    private WatchdogContainerNode? container;

    public void FrameworkThreadInit() {
        ThreadSafety.AssertMainThread();

        Service.Log.Debug("Overlay::FrameworkThreadInit");
        overlayController = new OverlayController();
        container = new WatchdogContainerNode();

        overlayController.AddNode(container);
    }

    public void Dispose() {
        container?.Dispose();
        overlayController?.Dispose();
    }

    public void UpdateConfig(Config config) {
        ThreadSafety.AssertMainThread();

        if (container is null) {
            Service.Log.Warning("Got config update event, but the node container is not ready yet. Skipping.");
            return;
        }

        container.Configure(config.OverlayConfig);
    }

    public void UpdateNodes(List<Alert> alerts) {
        ThreadSafety.AssertMainThread();

        if (container is null) {
            Service.Log.Warning("Got node update event, but the node container is not ready yet. Skipping.");
            return;
        }

        if (ShowDummy) {
            alerts = new List<Alert>(alerts);
            alerts.Insert(0, Alert.Dummy);
        }

        var alertCount = alerts.Count;
        container.SetVisibleChildCount(alertCount);

        if (alertCount == 0)
            return;

        var overlayConfig = Plugin.Config.OverlayConfig;
        var direction = overlayConfig.LayoutDirection;
        var padding = overlayConfig.PanelPadding;
        var gap = overlayConfig.PanelGap;

        var currentPosition = new Vector2();
        for (var i = 0; i < alertCount; i++) {
            var element = alerts[i];
            var node = container.Children[i];
            node.Apply(overlayConfig, PanelPayload.From(element));

            if (direction is LayoutDirection.LeftDown or LayoutDirection.LeftUp)
                currentPosition -= new Vector2(node.ContentSize.X + padding.Horizontal + gap, 0);

            node.Position = currentPosition;

            if (direction is LayoutDirection.UpLeft or LayoutDirection.DownLeft)
                node.Position -= new Vector2(node.ContentSize.X + padding.Horizontal + gap, 0);

            currentPosition += direction switch {
                LayoutDirection.UpLeft or LayoutDirection.UpRight => new Vector2(0, -node.ContentSize.Y - padding.Vertical - gap),
                LayoutDirection.DownLeft or LayoutDirection.DownRight => new Vector2(0, node.ContentSize.Y + padding.Vertical + gap),
                LayoutDirection.RightDown or LayoutDirection.RightUp => new Vector2(node.ContentSize.X + padding.Horizontal + gap, 0),
                _ => Vector2.Zero,
            };

            if (overlayConfig.LayoutWrap == LayoutWrap.WrapAtSize)
                HandleWrapAtSize(overlayConfig, ref currentPosition, node);
        }

        if (Math.Abs(container.ScaleX - overlayConfig.Scale) > 0.001)
            container.Scale = new Vector2(overlayConfig.Scale, overlayConfig.Scale);
    }

    private void HandleWrapAtSize(OverlayConfig overlayConfig, ref Vector2 currentPosition, CurrencyNode node) {
        var limit = overlayConfig.LayoutWrapSize;
        var padding = overlayConfig.PanelPadding;
        var gap = overlayConfig.PanelGap;

        switch (overlayConfig.LayoutDirection) {
            case LayoutDirection.RightDown:
                if (currentPosition.X >= limit) {
                    currentPosition.X = 0;
                    currentPosition.Y += node.ContentSize.Y + padding.Vertical + gap;
                }
                break;

            case LayoutDirection.RightUp:
                if (currentPosition.X >= limit) {
                    currentPosition.X = 0;
                    currentPosition.Y -= node.ContentSize.Y + padding.Vertical + gap;
                }
                break;

            case LayoutDirection.LeftDown:
                if (-currentPosition.X >= limit) {
                    currentPosition.X = 0;
                    currentPosition.Y += node.ContentSize.Y + padding.Vertical + gap;
                }
                break;

            case LayoutDirection.LeftUp:
                if (-currentPosition.X >= limit) {
                    currentPosition.X = 0;
                    currentPosition.Y -= node.ContentSize.Y + padding.Vertical + gap;
                }
                break;

            case LayoutDirection.DownRight:
                if (currentPosition.Y >= limit) {
                    currentPosition.Y = 0;
                    currentPosition.X += node.ContentSize.X + padding.Horizontal + gap;
                }
                break;

            case LayoutDirection.DownLeft:
                if (currentPosition.Y >= limit) {
                    currentPosition.Y = 0;
                    currentPosition.X -= node.ContentSize.X + padding.Horizontal + gap;
                }
                break;

            case LayoutDirection.UpRight:
                if (-currentPosition.Y >= limit) {
                    currentPosition.Y = 0;
                    currentPosition.X += node.ContentSize.X + padding.Horizontal + gap;
                }
                break;

            case LayoutDirection.UpLeft:
                if (-currentPosition.Y >= limit) {
                    currentPosition.Y = 0;
                    currentPosition.X -= node.ContentSize.X + padding.Horizontal + gap;
                }
                break;
        }
    }

    public void ClearNodes() {
        ThreadSafety.AssertMainThread();

        if (container is null) {
            Service.Log.Warning("Got node update event, but the node container is not ready yet. Skipping.");
            return;
        }

        container.SetVisibleChildCount(0);
    }
}
