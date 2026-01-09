using CurrencyWatchdog.Configuration;

namespace CurrencyWatchdog.Utility;

public static class Utils {
    public static (uint IconId, string Name) GetBurdenDisplay(Burden burden) {
        var autoIcon = 60071u; // was: 61523u
        var autoName = "(Unnamed)";
        if (burden.Subjects is [var subject, ..] && Plugin.Evaluator.GetDetails(subject) is { IconId: not 0 } itemDetails) {
            autoIcon = itemDetails.IconId;
            autoName = itemDetails.Alias ?? itemDetails.Name;
        }
        if (burden.Subjects.Count > 1) {
            autoName += $" (+{burden.Subjects.Count - 1})";
        }

        return (autoIcon, burden.Name ?? autoName);
    }
}
