using Dalamud.Configuration;
using Dalamud.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Numerics;
using static CurrencyWatchdog.Expressions.SubjectExpression;

namespace CurrencyWatchdog.Configuration;

[Serializable]
public class Config : IPluginConfiguration {
    public int Version { get; set; }

    public bool Enabled { get; set; } = true;

    public OverlayConfig OverlayConfig { get; set; } = new();

    public PanelConfig PanelConfig { get; set; } = new();

    public ChatConfig ChatConfig { get; set; } = new();

    public List<Burden> Burdens { get; set; } = [];

    public Config Clone() {
        return new Config {
            Version = Version,
            Enabled = Enabled,
            OverlayConfig = OverlayConfig with { },
            PanelConfig = PanelConfig with { },
            ChatConfig = ChatConfig with { },
            Burdens = Burdens.Select(x => x.Clone()).ToList(),
        };
    }
}

[Serializable]
public record OverlayConfig {
    public bool Enabled { get; set; } = true;
    public bool HideInDuty { get; set; } = true;
    public Vector2 Position { get; set; } = new(400f, 200f);
    public float Scale { get; set; } = 1f;
    public float IconSize { get; set; } = 32f;
    public LayoutDirection LayoutDirection { get; set; } = LayoutDirection.Right;
    public IconSideType IconSide { get; set; } = IconSideType.Left;
    public GameFont QuantityFont { get; set; } = GameFont.Axis;
    public FontOutlineType QuantityFontOutline { get; set; } = FontOutlineType.Strong;
    public uint QuantityFontSize { get; set; } = 12;
    public Vector2 QuantityNodeOffset { get; set; } = new(0f, 4f);
    public GameFont LabelFont { get; set; } = GameFont.Axis;
    public FontOutlineType LabelFontOutline { get; set; } = FontOutlineType.Normal;
    public uint LabelFontSize { get; set; } = 14;
    public float PanelGap { get; set; } = 2;
    public Spacing PanelPadding { get; set; } = new(2, 2, 2, 2);
    public HorizontalSpacing LabelPadding { get; set; } = new(2, 2);
    public PanelSizingType PanelSizing { get; set; } = PanelSizingType.Auto;
    public float PanelWidth { get; set; } = 90f;
}

[Serializable]
public record PanelConfig {
    public string QuantityTemplate { get; set; } = "{h}";
    public Vector4 QuantityColor { get; set; } = KnownColor.White.Vector();
    public Vector4 QuantityOutlineColor { get; set; } = KnownColor.Black.Vector();
    public string LabelTemplate { get; set; } = "{a}";
    public Vector4 LabelColor { get; set; } = KnownColor.White.Vector();
    public Vector4 LabelOutlineColor { get; set; } = KnownColor.Black.Vector();
    public Vector4 BackdropColor { get; set; } = KnownColor.Black.Vector() with { W = 0.5f };
}

[Serializable]
public record ChatConfig {
    public bool Enabled { get; set; } = true;

    public ChatAlertZoneAction LoginAction { get; set; } = ChatAlertZoneAction.None;
    public ChatAlertZoneAction ZoneAction { get; set; } = ChatAlertZoneAction.None;
    public ChatAlertUpdateAction AlertUpdateAction { get; set; } = ChatAlertUpdateAction.New;

    public bool PlaySound { get; set; } = true;
    public uint SoundEffectId { get; set; } = 9;

    public string Prefix { get; set; } = "[Currency Watchdog] ";
    public Vector4 PrefixColor { get; set; } = KnownColor.HotPink.Vector();
    public Vector4 PrefixOutlineColor { get; set; } = KnownColor.Black.Vector();

    public string MessageTemplate { get; set; } = "{a}: ";
    public Vector4 MessageColor { get; set; } = KnownColor.Gold.Vector();
    public Vector4 MessageOutlineColor { get; set; } = KnownColor.Black.Vector();

    public string SuffixTemplate { get; set; } = "{h,} / {c,}";
    public Vector4 SuffixColor { get; set; } = KnownColor.DarkOrange.Vector();
    public Vector4 SuffixOutlineColor { get; set; } = KnownColor.Black.Vector();
}

[Serializable]
public class Burden {
    public Guid Guid { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = true;
    public string? Name { get; set; }
    public List<Subject> Subjects { get; set; } = [];
    public List<Rule> Rules { get; set; } = [];

    public Burden Clone() {
        return new Burden {
            Guid = Guid.NewGuid(),
            Enabled = Enabled,
            Name = Name,
            Subjects = Subjects.Select(x => x with { }).ToList(),
            Rules = Rules.Select(x => x.Clone()).ToList(),
        };
    }
}

[Serializable]
public record Subject {
    public SubjectType Type { get; set; } = SubjectType.Item;
    public uint Id { get; set; }
    public string? Alias { get; set; }
    public bool Enabled { get; set; } = true;
}

[Serializable]
public class Rule {
    public Guid Guid { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = true;
    public List<Cond> Conds { get; set; } = [];
    public bool ShowPanel { get; set; } = true;
    public RulePanelConfig? PanelConfig;
    public bool ShowChat { get; set; } = true;
    public RuleChatConfig? ChatConfig;

    public Rule Clone() {
        return new Rule {
            Guid = Guid.NewGuid(),
            Enabled = Enabled,
            Conds = Conds.Select(x => x with { }).ToList(),
            ShowPanel = ShowPanel,
            PanelConfig = PanelConfig is { } p ? p with { } : null,
            ShowChat = ShowChat,
            ChatConfig = ChatConfig is { } c ? c with { } : null,
        };
    }
}

[Serializable]
public record RulePanelConfig {
    public string? QuantityTemplate { get; set; }
    public Vector4? QuantityColor { get; set; }
    public Vector4? QuantityOutlineColor { get; set; }
    public string? LabelTemplate { get; set; }
    public Vector4? LabelColor { get; set; }
    public Vector4? LabelOutlineColor { get; set; }
    public Vector4? BackdropColor { get; set; }
}

[Serializable]
public record RuleChatConfig {
    public string? Message { get; set; }
    public Vector4? MessageColor { get; set; }
    public Vector4? MessageOutlineColor { get; set; }
    public string? Suffix { get; set; }
    public Vector4? SuffixColor { get; set; }
    public Vector4? SuffixOutlineColor { get; set; }
}

[Serializable]
public enum LayoutDirection {
    [Display(Name = "Right")]
    Right,
    [Display(Name = "Left")]
    Left,
    [Display(Name = "Down (text growing right)")]
    DownRight,
    [Display(Name = "Down (text growing left)")]
    DownLeft,
    [Display(Name = "Up (text growing right)")]
    UpRight,
    [Display(Name = "Up (text growing left)")]
    UpLeft,
}

[Serializable]
public enum IconSideType {
    Left,
    Right,
}

[Serializable]
public enum SubjectType {
    [Display(Name = "Item")]
    Item,
    [Display(Name = "Grand Company Seal")]
    GrandCompanySeal,
    [Display(Name = "Evergreen Tomestone")]
    EvergreenTomestone,
    [Display(Name = "Discontinued Tomestone")]
    DiscontinuedTomestone,
    [Display(Name = "Standard Tomestone")]
    StandardTomestone,
    [Display(Name = "Limited Tomestone")]
    LimitedTomestone,
    [Display(Name = "Discontinued Crafters' Scrip")]
    DiscontinuedCraftersScrip,
    [Display(Name = "Discontinued Gatherers' Scrip")]
    DiscontinuedGatherersScrip,
    [Display(Name = "Previous Crafters' Scrip")]
    PreviousCraftersScrip,
    [Display(Name = "Previous Gatherers' Scrip")]
    PreviousGatherersScrip,
    [Display(Name = "Current Crafters' Scrip")]
    CurrentCraftersScrip,
    [Display(Name = "Current Gatherers' Scrip")]
    CurrentGatherersScrip,
}

[Serializable]
public record Spacing(float Left, float Right, float Top, float Bottom) {
    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;
}

[Serializable]
public record HorizontalSpacing(float Left, float Right) {
    public static readonly HorizontalSpacing Zero = new(0, 0);
    public float Total => Left + Right;
}

[Serializable]
public enum ChatAlertZoneAction {
    [Display(Name = "Do nothing")]
    None,
    [Display(Name = "Display all active alerts")]
    All,
}

[Serializable]
public enum ChatAlertUpdateAction {
    [Display(Name = "Do nothing")]
    None,
    [Display(Name = "Display all active alerts")]
    All,
    [Display(Name = "Display newly added alerts")]
    New,
}

[Serializable]
public enum GameFont {
    Axis,
    MiedingerMed,
    Miedinger,
    TrumpGothic,
    Jupiter,
    JupiterLarge,
}

[Serializable]
public enum FontOutlineType {
    None,
    Normal,
    Strong,
}

[Serializable]
public enum PanelSizingType {
    Auto,
    Fixed,
}
