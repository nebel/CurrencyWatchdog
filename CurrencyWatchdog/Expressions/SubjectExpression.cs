using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CurrencyWatchdog.Expressions;

[JsonDerivedType(typeof(Constant), typeDiscriminator: "Constant")]
[JsonDerivedType(typeof(Metric), typeDiscriminator: "Metric")]
public abstract record SubjectExpression {
    [Serializable]
    public enum MetricType {
        [Display(Name = "Cap")]
        Cap,
        [Display(Name = "Held")]
        QuantityHeld,
        [Display(Name = "Held (%)")]
        QuantityHeldPercentage,
        [Display(Name = "Missing")]
        QuantityMissing,
        [Display(Name = "Limited Cap")]
        LimitedCap,
        [Display(Name = "Limited Held")]
        LimitedQuantityHeld,
        [Display(Name = "Limited Held (%)")]
        LimitedQuantityHeldPercentage,
        [Display(Name = "Limited Missing")]
        LimitedQuantityMissing,
    }

    [Serializable]
    public record Constant(uint Value) : SubjectExpression;

    [Serializable]
    public record Metric(MetricType Type) : SubjectExpression;

    [Serializable]
    public enum Operator {
        [Display(Name = ">")]
        GreaterThan,
        [Display(Name = "≥")]
        GreaterThanOrEqualTo,
        [Display(Name = "=")]
        EqualTo,
        [Display(Name = "≤")]
        LessThanOrEqualTo,
        [Display(Name = "<")]
        LessThan,
    }

    [Serializable]
    public record Cond(SubjectExpression Left, Operator Operator, SubjectExpression Right, bool Negate = false);
}
