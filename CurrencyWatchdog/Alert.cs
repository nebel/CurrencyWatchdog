using CurrencyWatchdog.Configuration;
using CurrencyWatchdog.Expressions;
using System;
using System.Collections.Generic;

namespace CurrencyWatchdog;

public record AlertId(Guid Guid, int SubjectIndex);

public record Alert(AlertId AlertId, Rule ActiveRule, SubjectDetails SubjectDetails) {
    public static readonly Alert Dummy = GetDummyAlert();

    private static Alert GetDummyAlert() {
        var alertId = new AlertId(Guid.Empty, 0);
        var rule = new Rule {
            Conds = [
                new SubjectExpression.Cond(
                    new SubjectExpression.Metric(SubjectExpression.MetricType.QuantityHeld),
                    SubjectExpression.Operator.GreaterThanOrEqualTo,
                    new SubjectExpression.Constant(0)
                ),
            ],
        };
        var itemDetails = new SubjectDetails {
            Name = "Dummy",
            IconId = 105,
            Cap = 100,
            EffectiveCap = 100,
            QuantityHeld = 99,
        };
        return new Alert(alertId, rule, itemDetails);
    }
}

public sealed class AlertComparer : IEqualityComparer<Alert> {
    public bool Equals(Alert? a, Alert? b) {
        if (ReferenceEquals(a, b)) return true;
        return a is not null
               && b is not null
               && a.GetType() == b.GetType()
               && a.AlertId.Guid == b.AlertId.Guid
               && a.SubjectDetails.Cap == b.SubjectDetails.Cap
               && a.SubjectDetails.QuantityHeld == b.SubjectDetails.QuantityHeld
               && a.SubjectDetails.LimitedCap == b.SubjectDetails.LimitedCap
               && a.SubjectDetails.LimitedQuantityHeld == b.SubjectDetails.LimitedQuantityHeld;
    }

    public int GetHashCode(Alert obj) =>
        HashCode.Combine(obj.AlertId,
            obj.SubjectDetails.QuantityHeld,
            obj.SubjectDetails.Cap,
            obj.SubjectDetails.LimitedQuantityHeld,
            obj.SubjectDetails.LimitedCap);
}
