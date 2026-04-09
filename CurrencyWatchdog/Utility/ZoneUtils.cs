using Dalamud.Utility;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;
using TerritoryIntendedUse = FFXIVClientStructs.FFXIV.Client.Enums.TerritoryIntendedUse;

namespace CurrencyWatchdog.Utility;

public static class ZoneUtils {
    public static string GetName(TerritoryType t) {
        return t.PlaceName.Value.Name.ToString();
    }

    public static string GetInternalName(TerritoryType t) {
        return t.Name.ToString();
    }

    public static string GetName(ContentFinderCondition cfc) {
        return cfc.Name.ToString().FirstCharToUpper();
    }

    public static string GetTypeAndCategory(List<ContentFinderCondition> cfcList) {
        return string.Join(", ", cfcList.Select(GetTypeAndCategory).Distinct());
    }

    public static string GetTypeAndCategory(ContentFinderCondition cfc) {
        var type = GetContentTypeName(cfc);
        var category = GetContentCategoryName(cfc);

        if (type is not null && category is not null) {
            return $"{type}: {category}";
        }

        if (type is not null) {
            return type;
        }

        if (category is not null) {
            return category;
        }

        return "";
    }

    private static string? GetContentTypeName(ContentFinderCondition cfc) {
        if (cfc.ContentType is { RowId: not 0, ValueNullable.Name.IsEmpty: false }) {
            return cfc.ContentType.Value.Name.ToString();
        }
        return null;
    }

    private static string? GetContentCategoryName(ContentFinderCondition cfc) {
        if (cfc.ContentUICategory is { RowId: not 0, ValueNullable.Name.IsEmpty: false }) {
            return cfc.ContentUICategory.Value.Name.ToString();
        }
        return null;
    }


    public static ZoneDetails? GetDetails(TerritoryType t) {
        if (t.PlaceName.RowId is 0)
            return null;

        if (t.LoadingImage.RowId is 0 || !t.LoadingImage.IsValid)
            return null;

        if (!t.IsInUse)
            return null;

        if ((TerritoryIntendedUse)t.TerritoryIntendedUse.RowId is
            TerritoryIntendedUse.MordionGaol
            or TerritoryIntendedUse.OpeningArea
            or TerritoryIntendedUse.SoloOverworldInstances
           )
            return null;

        if (t.ContentFinderCondition is { IsValid: true, RowId: not 0, ValueNullable.Name: var name }) {
            if (name.IsEmpty)
                return null;
            return new ZoneDetails(t, [t.ContentFinderCondition.Value]);
        }

        var viaContent = Service.DataManager.GetExcelSheet<ContentFinderCondition>()
            .Where(c => c.TerritoryType is { IsValid: true, RowId: not 0 })
            .Where(c => c.TerritoryType.RowId == t.RowId)
            .Where(c => !c.Name.IsEmpty)
            .ToList();

        return new ZoneDetails(t, viaContent);
    }
}

public record ZoneDetails(TerritoryType TerritoryType, List<ContentFinderCondition> ContentFinderConditions) {
    public string ZoneSearch => Utils.NormalizeForSearch(TerritoryType.PlaceName.Value.Name.ToString());
    public string ContentSearch => Utils.NormalizeForSearch(ZoneUtils.GetTypeAndCategory(ContentFinderConditions));
}
