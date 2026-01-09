using System;
using static CurrencyWatchdog.Expressions.SubjectExpression;

namespace CurrencyWatchdog.Expressions;

public static class ExpressionEvaluator {
    public static bool EvaluateCond(Cond cond, SubjectDetails details) {
        var left = ResolveExpression(cond.Left, details);
        var right = ResolveExpression(cond.Right, details);

        var result = cond.Operator switch {
            Operator.GreaterThan => left > right,
            Operator.GreaterThanOrEqualTo => left >= right,
            Operator.EqualTo => left == right,
            Operator.LessThanOrEqualTo => left <= right,
            Operator.LessThan => left < right,
            _ => false,
        };

        return cond.Negate ? !result : result;
    }

    private static uint ResolveExpression(SubjectExpression expr, SubjectDetails details) {
        return expr switch {
            Metric m => ResolveMetric(m.Type, details),
            Constant c => c.Value,
            _ => 0,
        };
    }

    private static uint ResolveMetric(MetricType type, SubjectDetails details) {
        return type switch {
            MetricType.Cap => details.Cap,
            MetricType.QuantityHeld => details.QuantityHeld,
            MetricType.QuantityHeldPercentage => details.QuantityHeldPercentage,
            MetricType.QuantityMissing => details.QuantityMissing,
            MetricType.LimitedCap => details.LimitedCap ?? 0,
            MetricType.LimitedQuantityHeld => details.LimitedQuantityHeld ?? 0,
            MetricType.LimitedQuantityHeldPercentage => details.LimitedQuantityHeldPercentage ?? 0,
            MetricType.LimitedQuantityMissing => details.LimitedQuantityMissing ?? 0,
            _ => throw new ArgumentException($"Unsupported metric type: {type}"),
        };
    }
}
