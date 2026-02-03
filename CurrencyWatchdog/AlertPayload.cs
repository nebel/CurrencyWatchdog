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
            Message = RenderPlaceholders(alert, local?.Message ?? global.MessageTemplate),
            MessageColor = local?.MessageColor ?? global.MessageColor,
            MessageOutlineColor = local?.MessageOutlineColor ?? global.MessageOutlineColor,
            Suffix = RenderPlaceholders(alert, local?.Suffix ?? global.SuffixTemplate),
            SuffixColor = local?.SuffixColor ?? global.SuffixColor,
            SuffixOutlineColor = local?.SuffixOutlineColor ?? global.SuffixOutlineColor,
        };
    }
}

public record PanelPayload {
    public required Icon Icon;
    public required string QuantityTemplate;
    public required Vector4 QuantityColor;
    public required Vector4 QuantityOutlineColor;
    public required string LabelTemplate;
    public required Vector4 LabelColor;
    public required Vector4 LabelOutlineColor;
    public required Vector4 BackdropColor;

    public static PanelPayload From(Alert alert) {
        var local = alert.ActiveRule.PanelConfig;
        var global = Plugin.Config.PanelConfig;
        return new PanelPayload {
            Icon = new Icon(alert.SubjectDetails.IconId, alert.SubjectDetails.UseHqIcon),
            QuantityTemplate = RenderPlaceholders(alert, local?.QuantityTemplate ?? global.QuantityTemplate),
            QuantityColor = local?.QuantityColor ?? global.QuantityColor,
            QuantityOutlineColor = local?.QuantityOutlineColor ?? global.QuantityOutlineColor,
            LabelTemplate = RenderPlaceholders(alert, local?.LabelTemplate ?? global.LabelTemplate),
            LabelColor = local?.LabelColor ?? global.LabelColor,
            LabelOutlineColor = local?.LabelOutlineColor ?? global.LabelOutlineColor,
            BackdropColor = local?.BackdropColor ?? global.BackdropColor,
        };
    }
}

public static partial class PayloadUtils {
    private static readonly string[] UpperSuffixes = ["", "K", "M", "G", "T"];
    private static readonly string[] LowerSuffixes = ["", "k", "m", "g", "t"];

    // ReSharper disable once StringLiteralTypo
    [GeneratedRegex(@"\{(?<key>[nachpmNCHPM])(?<fmt>:[^}]{0,20})?\}")]
    private static partial Regex PlaceholderRegex();

    public static string RenderPlaceholders(Alert alert, string text) {
        if (string.IsNullOrEmpty(text))
            return text;

        var d = alert.SubjectDetails;

        return PlaceholderRegex().Replace(text, m => {
            var key = m.Groups["key"].Value[0];
            var spec = m.Groups["fmt"] is { Success: true, Value: var fmtVal } ? fmtVal[1..] : null;
            return key switch {
                'n' => d.Name,
                'a' => d.Alias ?? d.Name,
                'c' => FormatSpec(d.EffectiveCap, spec),
                'h' => FormatSpec(d.QuantityHeld, spec),
                'p' => FormatSpec(d.QuantityHeldPercentage, spec),
                'm' => FormatSpec(d.QuantityMissing, spec),
                'C' => FormatSpec(d.LimitedCap, spec),
                'H' => FormatSpec(d.LimitedQuantityHeld, spec),
                'P' => FormatSpec(d.LimitedQuantityHeldPercentage, spec),
                'M' => FormatSpec(d.LimitedQuantityMissing, spec),
                _ => m.Value,
            };
        });
    }

    private static string FormatSpec(decimal? value, string? spec) {
        if (value is not { } num) return "?";

        if (spec == null) {
            return decimal.Floor(num).ToString("0");
        }

        if (spec.StartsWith('^')) {
            num = decimal.Ceiling(num);
            spec = spec[1..];
        } else if (spec.StartsWith('_')) {
            num = decimal.Floor(num);
            spec = spec[1..];
        }

        var upperZ = spec.StartsWith('Z');
        var lowerZ = spec.StartsWith('z');
        if (lowerZ || upperZ) {
            int digits;

            if (spec.Length == 1) {
                digits = 3;
            } else if (!int.TryParse(spec.AsSpan(1), out digits)) {
                return "<ERR>";
            }

            try {
                return FormatScaled(num, digits, lowerZ ? LowerSuffixes : UpperSuffixes);
            } catch {
                return "<ERR>";
            }
        }

        try {
            return num.ToString(spec);
        } catch {
            return "<ERR>";
        }
    }

    private static string FormatScaled(decimal value, int significantDigits, string[] suffixes) {
        if (value == 0m)
            return "0";

        var absValue = Math.Abs(value);
        var suffixIndex = 0;

        while (absValue >= 1000m && suffixIndex < suffixes.Length - 1) {
            absValue /= 1000m;
            suffixIndex++;
        }

        var digitsBeforeDecimal =
            absValue >= 100m ? 3 :
            absValue >= 10m ? 2 :
            1;

        var decimals = Math.Clamp(significantDigits - digitsBeforeDecimal, 0, 28);

        var rounded =
            decimal.Round(absValue, decimals, MidpointRounding.AwayFromZero);

        return (value < 0 ? "-" : "") +
               rounded.ToString($"F{decimals}", CultureInfo.InvariantCulture)
                   .TrimEnd('0')
                   .TrimEnd('.') +
               suffixes[suffixIndex];
    }
}
