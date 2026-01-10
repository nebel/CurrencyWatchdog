using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Expressions;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;

namespace CurrencyWatchdog;

public class Evaluator {
    private const uint DiscontinuedCraftersScripId = 25199;
    private const uint DiscontinuedGatherersScripId = 25200;
    private const uint PreviousCraftersScripId = 33913;
    private const uint PreviousGatherersScripId = 33914;
    private const uint CurrentCraftersScripId = 41784;
    private const uint CurrentGatherersScripId = 41785;

    private readonly uint evergreenTomestoneId;
    private readonly uint discontinuedTomestoneId;
    private readonly uint standardTomestoneId;
    private readonly uint limitedTomestoneId;

    private readonly uint weeklyTomestoneLimit;

    public Evaluator() {
        foreach (var item in Service.DataManager.Excel.GetSheet<TomestonesItem>()) {
            if (item.Tomestones.ValueNullable is { WeeklyLimit: var limit and > 0 }) {
                limitedTomestoneId = item.Item.RowId;
                weeklyTomestoneLimit = limit;
            } else if (item.Tomestones is { RowId: 1 }) {
                evergreenTomestoneId = item.Item.RowId;
            } else if (item.Tomestones is { RowId: 2 }) {
                standardTomestoneId = item.Item.RowId;
            } else if (item.Tomestones is { RowId: 4 }) {
                discontinuedTomestoneId = item.Item.RowId;
            }
        }

        // // Need to be logged in for the following to work, so let's just hard-code it since it's a once-per-expansion thing.
        // unsafe {
        //     var cm = CurrencyManager.Instance();
        //     DiscontinuedCraftersScripId = cm->GetItemIdBySpecialId(1);
        //     DiscontinuedGatherersScripId = cm->GetItemIdBySpecialId(3);
        //     PreviousCraftersScripId = cm->GetItemIdBySpecialId(2);
        //     PreviousGatherersScripId = cm->GetItemIdBySpecialId(4);
        //     CurrentCraftersScripId = cm->GetItemIdBySpecialId(6);
        //     CurrentGatherersScripId = cm->GetItemIdBySpecialId(7);
        // }
    }

    public (List<Alert> PanelAlerts, List<Alert> ChatAlerts) Evaluate(List<Burden> burdens) {
        var panelAlerts = new List<Alert>();
        var chatAlerts = new List<Alert>();

        foreach (var burden in burdens) {
            if (!burden.Enabled)
                continue;

            for (var subjectIndex = 0; subjectIndex < burden.Subjects.Count; subjectIndex++) {
                if (GetDetails(burden.Subjects[subjectIndex]) is { } itemDetails) {
                    var foundRule = FindMatchingRule(burden, itemDetails);
                    if (foundRule is null or { ShowChat: false, ShowPanel: false })
                        continue;

                    var alertId = new AlertId(foundRule.Guid, subjectIndex);
                    var alert = new Alert(alertId, foundRule, itemDetails);

                    if (Plugin.Config.OverlayConfig.Enabled && foundRule.ShowPanel)
                        panelAlerts.Add(alert);
                    if (Plugin.Config.ChatConfig.Enabled && foundRule.ShowChat)
                        chatAlerts.Add(alert);
                }
            }
        }

        return (panelAlerts, chatAlerts);
    }

    private static Rule? FindMatchingRule(Burden burden, SubjectDetails subjectDetails) {
        // Service.Log.Debug($"Subject: {subjectDetails}");
        foreach (var rule in burden.Rules) {
            if (!rule.Enabled)
                continue;

            foreach (var cond in rule.Conds) {
                if (ExpressionEvaluator.EvaluateCond(cond, subjectDetails))
                    return rule;
            }
        }

        return null;
    }

    public SubjectDetails? GetDetails(Subject subject) {
        return subject.Type switch {
            SubjectType.Item =>
                GetItemDetails(subject),
            SubjectType.GrandCompanySeal =>
                GetSealDetails(subject),
            SubjectType.EvergreenTomestone or SubjectType.DiscontinuedTomestone or SubjectType.StandardTomestone or SubjectType.LimitedTomestone =>
                GetTomestoneDetails(subject),
            SubjectType.DiscontinuedCraftersScrip => GetItemDetails(subject with { Id = DiscontinuedCraftersScripId }),
            SubjectType.DiscontinuedGatherersScrip => GetItemDetails(subject with { Id = DiscontinuedGatherersScripId }),
            SubjectType.PreviousCraftersScrip => GetItemDetails(subject with { Id = PreviousCraftersScripId }),
            SubjectType.PreviousGatherersScrip => GetItemDetails(subject with { Id = PreviousGatherersScripId }),
            SubjectType.CurrentCraftersScrip => GetItemDetails(subject with { Id = CurrentCraftersScripId }),
            SubjectType.CurrentGatherersScrip => GetItemDetails(subject with { Id = CurrentGatherersScripId }),
            _ => throw new ArgumentOutOfRangeException($"Unsupported subject type: {subject.Type}"),
        };
    }

    private SubjectDetails GetTomestoneDetails(Subject subject) {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        var tomestoneItemId = subject.Type switch {
            SubjectType.EvergreenTomestone => evergreenTomestoneId,
            SubjectType.DiscontinuedTomestone => discontinuedTomestoneId,
            SubjectType.StandardTomestone => standardTomestoneId,
            SubjectType.LimitedTomestone => limitedTomestoneId,
            _ => throw new ArgumentOutOfRangeException($"Unsupported subject type (non-tomestone): {subject.Type}"),
        };

        if (tomestoneItemId == 0)
            return UnreleasedTomestone();

        return GetItemDetails(subject with { Id = tomestoneItemId }) ?? UnreleasedTomestone();
    }

    private static SubjectDetails UnreleasedTomestone() {
        return new SubjectDetails {
            Name = "(Unreleased Tomestone)",
            IconId = 65012,
            Cap = 2000,
            QuantityHeld = 0,
        };
    }

    private SubjectDetails GetSealDetails(Subject subject) {
        if (Service.PlayerState.GrandCompany.ValueNullable is { } grandCompany) {
            var rankRow = Service.PlayerState.GetGrandCompanyRank(grandCompany);
            if (Service.DataManager.Excel.GetSheet<GrandCompanyRank>().GetRowOrDefault(rankRow) is { } rank) {
                if (GetGrandCompanySealItemId(grandCompany) is var itemId and not 0) {
                    if (GetItemDetails(subject with { Id = itemId }) is { } details) {
                        return details with { Cap = rank.MaxSeals };
                    }
                }
            }
        }

        return GenericGrandCompanySeal();
    }

    private static uint GetGrandCompanySealItemId(GrandCompany grandCompany) {
        return grandCompany.RowId switch {
            1 => 20,
            2 => 21,
            3 => 22,
            _ => 0,
        };
    }

    private static SubjectDetails GenericGrandCompanySeal() {
        return new SubjectDetails {
            Name = "(Generic Grand Company Seal)",
            IconId = 65004,
            Cap = 10000,
            QuantityHeld = 0,
        };
    }

    private static unsafe uint GetCount(uint itemId) {
        return InventoryManager.Instance()->GetInventoryItemCount(itemId) is var count and >= 0 ? (uint)count : 0;
    }

    private unsafe SubjectDetails? GetItemDetails(Subject subject) {
        var itemId = subject.Id;
        if (Service.DataManager.Excel.GetSheet<Item>().GetRowOrDefault(itemId) is not { } item)
            return null;

        if (item.RowId == 0)
            return null;

        var name = item.Name.ToString();
        var quantity = GetCount(itemId);
        var cap = item.StackSize;

        if (itemId == limitedTomestoneId) {
            return new SubjectDetails {
                Name = name,
                Alias = subject.Alias,
                IconId = item.Icon,
                QuantityHeld = quantity,
                Cap = cap,
                LimitedQuantityHeld = (uint)InventoryManager.Instance()->GetWeeklyAcquiredTomestoneCount(),
                LimitedCap = weeklyTomestoneLimit,
            };
        }

        return new SubjectDetails {
            Name = name,
            Alias = subject.Alias,
            IconId = item.Icon,
            QuantityHeld = quantity,
            Cap = cap,
        };
    }
}

public record SubjectDetails {
    public required string Name;
    public string? Alias;
    public required uint IconId;
    public required uint Cap;
    public required uint QuantityHeld;
    public uint? LimitedCap;
    public uint? LimitedQuantityHeld;

    public decimal QuantityHeldPercentage {
        get {
            if (QuantityHeld > Cap)
                return 100;
            if (Cap is not 0)
                return (decimal)QuantityHeld * 100 / Cap;
            return 0;
        }
    }

    public uint QuantityMissing => QuantityHeld > Cap ? 0 : Cap - QuantityHeld;

    public decimal? LimitedQuantityHeldPercentage {
        get {
            if (LimitedCap is { } limitedCap and not 0 && LimitedQuantityHeld is { } limitedQuantityHeld) {
                if (limitedQuantityHeld > limitedCap)
                    return 100;
                return (decimal)limitedQuantityHeld * 100 / limitedCap;
            }
            return null;
        }
    }

    public uint? LimitedQuantityMissing {
        get {
            if (LimitedCap is { } limitedCap && LimitedQuantityHeld is { } limitedQuantityHeld) {
                if (limitedQuantityHeld > limitedCap)
                    return 0;
                return limitedCap - limitedQuantityHeld;
            }
            return null;
        }
    }
}
