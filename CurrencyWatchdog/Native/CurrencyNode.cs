using CurrencyWatchdog.Configuration;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using System;
using System.Drawing;
using System.Numerics;

namespace CurrencyWatchdog.Native;

public class CurrencyNode : SimpleOverlayNode {
    private readonly BackgroundImageNode backdropNode;
    private readonly IconImageNode iconImageNode;
    private readonly TextNode quantityNode;
    private readonly TextNode labelNode;

    private HorizontalSpacing currentTextPadding = HorizontalSpacing.Zero;

    public CurrencyNode() {
        backdropNode = new BackgroundImageNode {
            Color = new Vector4(0, 0, 0, 0.5f),
            Size = new Vector2(36.0f, 36.0f),
        };
        backdropNode.AttachNode(this);

        iconImageNode = new IconImageNode {
            IconId = 1,
            Size = new Vector2(36.0f, 36.0f),
            FitTexture = true,
        };
        iconImageNode.AttachNode(this);

        quantityNode = new TextNode {
            BackgroundColor = Vector4.Zero,
            Size = new Vector2(36.0f, 36.0f),
            Position = new Vector2(0f, 4f),
            FontType = FontType.Axis,
            AlignmentType = AlignmentType.Bottom,
            FontSize = 12,
            LineSpacing = 14,
            TextColor = KnownColor.White.Vector(),
            TextOutlineColor = KnownColor.Black.Vector(),
            TextFlags = TextFlags.AutoAdjustNodeSize | TextFlags.Edge,
        };
        quantityNode.AttachNode(this);

        labelNode = new TextNode {
            BackgroundColor = Vector4.Zero,
            Size = new Vector2(200.0f, 20.0f),
            Position = new Vector2(36.0f, 0.0f),
            FontType = FontType.Axis,
            AlignmentType = AlignmentType.Left,
            FontSize = 14,
            LineSpacing = 14,
            TextColor = KnownColor.White.Vector(),
            TextOutlineColor = KnownColor.Black.Vector(),
            TextFlags = TextFlags.AutoAdjustNodeSize | TextFlags.Edge,
        };
        labelNode.AttachNode(this);
    }

    public void Configure(OverlayConfig config) {
        var iconSize = new Vector2(config.IconSize, config.IconSize);

        if (config.PanelSizing == PanelSizingType.Fixed) {
            labelNode.RemoveTextFlags(TextFlags.AutoAdjustNodeSize);
            labelNode.AddTextFlags(TextFlags.Ellipsis);
            labelNode.Size = iconSize with { X = config.PanelWidth };
        } else {
            labelNode.RemoveTextFlags(TextFlags.Ellipsis);
            labelNode.AddTextFlags(TextFlags.AutoAdjustNodeSize);
            labelNode.Size = iconSize;
        }

        labelNode.FontType = ToFontType(config.LabelFont);
        labelNode.FontSize = config.LabelFontSize;
        ApplyOutlineType(config.LabelFontOutline, labelNode);

        iconImageNode.Size = iconSize;

        quantityNode.Size = iconSize;
        quantityNode.FontType = ToFontType(config.QuantityFont);
        quantityNode.FontSize = config.QuantityFontSize;
        ApplyOutlineType(config.QuantityFontOutline, quantityNode);

        backdropNode.Position = new Vector2(0 - config.PanelPadding.Left, 0 - config.PanelPadding.Top);
    }

    public void Apply(OverlayConfig config, PanelPayload payload) {
        IsVisible = true;

        var textPadding = !string.IsNullOrEmpty(payload.LabelTemplate) || config.PanelSizing == PanelSizingType.Fixed
            ? config.LabelPadding
            : HorizontalSpacing.Zero;
        currentTextPadding = textPadding;

        var iconSize = new Vector2(config.IconSize, config.IconSize);
        var labelPosition = config.IconSide == IconSideType.Left
            ? new Vector2(iconSize.X + textPadding.Left, 0.0f)
            : new Vector2(textPadding.Left, 0.0f);

        labelNode.Position = labelPosition;
        labelNode.String = payload.LabelTemplate;
        labelNode.TextColor = payload.LabelColor;
        labelNode.TextOutlineColor = payload.LabelOutlineColor;

        var iconPosition = config.IconSide == IconSideType.Left
            ? Vector2.Zero
            : new Vector2(labelNode.Size.X + textPadding.Total, 0.0f);

        iconImageNode.Position = iconPosition;
        iconImageNode.IconId = payload.Icon;

        quantityNode.Position = iconPosition + config.QuantityNodeOffset;
        quantityNode.String = payload.QuantityTemplate;
        quantityNode.TextColor = payload.QuantityColor;
        quantityNode.TextOutlineColor = payload.QuantityOutlineColor;

        var backdropSizeX = iconImageNode.Size.X
                            + labelNode.Size.X
                            + textPadding.Left + textPadding.Right
                            + config.PanelPadding.Left + config.PanelPadding.Right;
        var backdropSizeY = iconImageNode.Size.Y
                            + config.PanelPadding.Top + config.PanelPadding.Bottom;
        backdropNode.Size = new Vector2(backdropSizeX, backdropSizeY);
        backdropNode.Color = payload.BackdropColor;
    }

    public Vector2 ContentSize => iconImageNode.Size with { X = iconImageNode.Size.X + labelNode.Size.X + currentTextPadding.Left + currentTextPadding.Right };

    private static FontType ToFontType(GameFont font) {
        return font switch {
            GameFont.Axis => FontType.Axis,
            GameFont.MiedingerMed => FontType.MiedingerMed,
            GameFont.Miedinger => FontType.Miedinger,
            GameFont.TrumpGothic => FontType.TrumpGothic,
            GameFont.Jupiter => FontType.Jupiter,
            GameFont.JupiterLarge => FontType.JupiterLarge,
            _ => throw new ArgumentOutOfRangeException($"Unknown GameFont: {font}"),
        };
    }

    private static void ApplyOutlineType(FontOutlineType outlineType, TextNode node) {
        switch (outlineType) {
            case FontOutlineType.None:
                node.RemoveTextFlags(TextFlags.Edge, TextFlags.Glare);
                break;
            case FontOutlineType.Normal:
                node.RemoveTextFlags(TextFlags.Glare);
                node.AddTextFlags(TextFlags.Edge);
                break;
            case FontOutlineType.Strong:
                node.RemoveTextFlags(TextFlags.Edge);
                node.AddTextFlags(TextFlags.Glare);
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unknown FontOutlineType: {outlineType}");
        }
    }
}
