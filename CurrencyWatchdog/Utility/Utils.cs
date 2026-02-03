using CurrencyWatchdog.Configuration;

namespace CurrencyWatchdog.Utility;

public static class Utils {
    public const string UintDisplayFormat = "N0";
    public const string PercentDisplayFormat = "#,##0.##";
    public const string DecimalDisplayFormat = "#,##0.#########";
    public const decimal CustomConstantMax = 999_999_999_999m;

    public static (Icon Icon, string Name) GetBurdenDisplay(Burden burden) {
        var autoIcon = new Icon(60071u); // was: 61523u
        var autoName = "(Unnamed)";
        if (burden.Subjects is [var subject, ..] && Plugin.Evaluator.GetDetails(subject) is { IconId: not 0 } itemDetails) {
            autoIcon = new Icon(itemDetails.IconId, itemDetails.UseHqIcon);
            autoName = itemDetails.Alias ?? itemDetails.Name;
        }
        if (burden.Subjects.Count > 1) {
            autoName += $" (+{burden.Subjects.Count - 1})";
        }

        return (autoIcon, burden.Name ?? autoName);
    }
}
