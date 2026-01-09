using System.Collections.Generic;
using System.Linq;
using static CurrencyWatchdog.Expressions.SubjectExpression;

namespace CurrencyWatchdog.Configuration;

public static class Presets {
    public static readonly Dictionary<string, Burden> PresetBurdens = new() {
        ["GcSeals"] = new Burden {
            Subjects = [new Subject { Type = SubjectType.GrandCompanySeal }],
            Rules = [PercentRule(80)],
        },
        ["PvpTokens"] = new Burden {
            Name = "PvP Tokens",
            Subjects = [Item(25), Item(36656)], // Wolf Mark, Trophy Crystal
            Rules = [PercentRule(80)],
        },
        ["HuntTokens"] = new Burden {
            Name = "Hunt Tokens",
            Subjects = [Item(27), Item(10307), Item(26533)], // Allied Seal, Centurio Seal, Sack of Nuts
            Rules = [PercentRule(80)],
        },
        ["BicolorGemstones"] = new Burden {
            Subjects = [Item(26807)], // Bicolor Gemstone
            Rules = [PercentRule(80)],
        },
        ["Tomestones"] = new Burden {
            Name = "Tomestones",
            Subjects = [Special(SubjectType.EvergreenTomestone), Special(SubjectType.StandardTomestone), Special(SubjectType.LimitedTomestone)],
            Rules = [PercentRule(70)],
        },
        ["CraftersScrips"] = new Burden {
            Name = "Crafters' Scrips",
            Subjects = [Special(SubjectType.CurrentCraftersScrip), Special(SubjectType.PreviousCraftersScrip)],
            Rules = [PercentRule(80)],
        },
        ["GatherersScrips"] = new Burden {
            Name = "Gatherers' Scrips",
            Subjects = [Special(SubjectType.CurrentGatherersScrip), Special(SubjectType.PreviousGatherersScrip)],
            Rules = [PercentRule(80)],
        },
        ["SkybuildersScrips"] = new Burden {
            Subjects = [Item(28063)], // Skybuilders' Scrip
            Rules = [PercentRule(70)],
        },
        ["Crystals"] = new Burden {
            Name = "Crystals",
            Subjects = Enumerable.Range(2, 18).Select(Item).ToList(), // Fire Shard ... Water Cluster
            Rules = [PercentRule(90)],
        },
    };

    private static Subject Item(int itemId) => new() { Id = (uint)itemId };
    private static Subject Special(SubjectType type) => new() { Type = type };
    private static Rule PercentRule(uint percent) => new() {
        Conds = [new Cond(new Metric(MetricType.QuantityHeldPercentage), Operator.GreaterThanOrEqualTo, new Constant(percent))],
    };
}
