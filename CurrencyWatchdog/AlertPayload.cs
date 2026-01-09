using System;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using static CurrencyWatchdog.PayloadUtils;

namespace CurrencyWatchdog;

public record ChatPayload {
    public required string Message;
    public required Vector4 MessageColor;
    public required Vector4 MessageOutlineColor;

    public required string Suffix;
    public required Vector4 SuffixColor;
    public required Vector4 SuffixOutlineColor;

    public static ChatPayload From(Alert alert) {
        var local = alert.ActiveRule.ChatConfig;
        var global = Plugin.Config.ChatConfig;
        return new ChatPayload {
            Message = RenderPlaceholders(alert, local?.Message ?? global.MessageFormat),
            MessageColor = local?.MessageColor ?? global.MessageColor,
            MessageOutlineColor = local?.MessageOutlineColor ?? global.MessageOutlineColor,
            Suffix = RenderPlaceholders(alert, local?.Suffix ?? global.SuffixFormat),
            SuffixColor = local?.SuffixColor ?? global.SuffixColor,
            SuffixOutlineColor = local?.SuffixOutlineColor ?? global.SuffixOutlineColor,
        };
    }
}

public record PanelPayload {
    public required uint Icon;
    public required string QuantityFormat;
    public required Vector4 QuantityColor;
    public required Vector4 QuantityOutlineColor;
    public required string LabelFormat;
    public required Vector4 LabelColor;
    public required Vector4 LabelOutlineColor;
    public required Vector4 BackdropColor;

    public static PanelPayload From(Alert alert) {
        var local = alert.ActiveRule.PanelConfig;
        var global = Plugin.Config.PanelConfig;
        return new PanelPayload {
            Icon = alert.SubjectDetails.IconId,
            QuantityFormat = RenderPlaceholders(alert, local?.QuantityFormat ?? global.QuantityFormat),
            QuantityColor = local?.QuantityColor ?? global.QuantityColor,
            QuantityOutlineColor = local?.QuantityOutlineColor ?? global.QuantityOutlineColor,
            LabelFormat = RenderPlaceholders(alert, local?.LabelFormat ?? global.LabelFormat),
            LabelColor = local?.LabelColor ?? global.LabelColor,
            LabelOutlineColor = local?.LabelOutlineColor ?? global.LabelOutlineColor,
            BackdropColor = local?.BackdropColor ?? global.BackdropColor,
        };
    }
}

public static partial class PayloadUtils {
    private static readonly string[] Suffixes = ["", "K", "M", "G", "T"];

    // ReSharper disable once StringLiteralTypo
    [GeneratedRegex(@"\{(?<key>[nachpmNCHPM])(?<fmt>,|\.(?<prec>\d+)?)?\}")]
    private static partial Regex PlaceholderRegex();

    public static string RenderPlaceholders(Alert alert, string text) {
        if (string.IsNullOrEmpty(text))
            return text;

        var d = alert.SubjectDetails;

        return PlaceholderRegex().Replace(text, m => {
            var key = m.Groups["key"].Value[0];
            var fmt = m.Groups["fmt"] is { Success: true, Value: var fmtVal } ? fmtVal[0] : ' ';
            var precision = m.Groups["prec"] is { Success: true, Value: var precisionVal } ? int.Parse(precisionVal) : 3;

            return key switch {
                'n' => d.Name,
                'a' => d.Alias ?? d.Name,
                'c' => Format(d.Cap, fmt, precision),
                'h' => Format(d.QuantityHeld, fmt, precision),
                'p' => Format(d.QuantityHeldPercentage, fmt, precision),
                'm' => Format(d.QuantityMissing, fmt, precision),
                'C' => Format(d.LimitedCap, fmt, precision),
                'H' => Format(d.LimitedQuantityHeld, fmt, precision),
                'P' => Format(d.LimitedQuantityHeldPercentage, fmt, precision),
                'M' => Format(d.LimitedQuantityMissing, fmt, precision),
                _ => m.Value,
            };
        });
    }

    private static string Format(uint? value, char fmt, int precision) {
        if (value is not { } num) return "?";

        return fmt switch {
            '.' => FormatWithSuffix(num, precision),
            ',' => num.ToString("N0"),
            _ => num.ToString(),
        };
    }

    private static string FormatWithSuffix(double value, int significantDigits) {
        if (value == 0)
            return "0";

        var absValue = Math.Abs(value);
        var suffixIndex = 0;

        while (absValue >= 1000 && suffixIndex < Suffixes.Length - 1) {
            absValue /= 1000;
            suffixIndex++;
        }

        var digitsBeforeDecimal = (int)Math.Floor(Math.Log10(absValue)) + 1;
        var decimals = Math.Clamp(significantDigits - digitsBeforeDecimal, 0, 15);

        var rounded = Math.Round(absValue, decimals, MidpointRounding.AwayFromZero);

        return (value < 0 ? "-" : "") +
               rounded.ToString($"F{decimals}", CultureInfo.InvariantCulture)
                   .TrimEnd('0')
                   .TrimEnd('.') +
               Suffixes[suffixIndex];
    }
}
