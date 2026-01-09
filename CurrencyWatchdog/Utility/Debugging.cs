using CurrencyWatchdog.Configuration;
using Dalamud.Interface;
using System.Drawing;
using static CurrencyWatchdog.Expressions.SubjectExpression;

namespace CurrencyWatchdog.Utility;

public static class Debugging {
    private static bool IsDebug => false;

    private static bool UseDebugStorage => false;

    public static void PostInit() {
        if (IsDebug) {
            Plugin.WindowManager.ConfigWindow.IsOpen = true;
        }
    }

    public static StorageContext<Config>? GetDebugStorage() {
        if (IsDebug && UseDebugStorage) {
            return new StorageContext<Config>("Config", "Debug") {
                AllowLoad = false,
                AllowSave = false,
                Fallback = CreateDebugConfig,
            };
        }
        return null;
    }

    private static Config CreateDebugConfig() {
        return new Config {
            Burdens = [
                new Burden {
                    Subjects = [new Subject { Id = 1 }],
                    Rules = [
                        new Rule {
                            Conds = [new Cond(new Metric(MetricType.QuantityHeld), Operator.EqualTo, new Metric(MetricType.Cap))],
                            PanelConfig = new RulePanelConfig { QuantityColor = KnownColor.Crimson.Vector() },
                        },
                        new Rule {
                            Conds = [new Cond(new Metric(MetricType.QuantityHeldPercentage), Operator.GreaterThanOrEqualTo, new Constant(90))],
                            PanelConfig = new RulePanelConfig { QuantityColor = KnownColor.Orange.Vector() },
                        },
                        new Rule {
                            Conds = [new Cond(new Metric(MetricType.QuantityHeld), Operator.GreaterThanOrEqualTo, new Constant(5000))],
                            PanelConfig = new RulePanelConfig { QuantityColor = KnownColor.GreenYellow.Vector(), QuantityFormat = "{h.2}" },
                        },
                    ],
                },
                new Burden {
                    Subjects = [new Subject { Type = SubjectType.GrandCompanySeal }],
                    Rules = [
                        new Rule {
                            Conds = [new Cond(new Metric(MetricType.QuantityHeldPercentage), Operator.GreaterThanOrEqualTo, new Constant(50))],
                            PanelConfig = new RulePanelConfig { QuantityColor = KnownColor.MediumAquamarine.Vector(), QuantityFormat = "{h.}" },
                        },
                    ],
                },
                new Burden {
                    Subjects = [
                        new Subject { Id = 4868, Alias = "Greens" },
                        new Subject { Id = 21069, Alias = "GC Ticket" },
                    ],
                    Rules = [
                        new Rule {
                            Conds = [new Cond(new Metric(MetricType.QuantityHeld), Operator.GreaterThanOrEqualTo, new Constant(1))],
                            PanelConfig = new RulePanelConfig { QuantityColor = KnownColor.MediumPurple.Vector() },
                        },
                    ],
                },
            ],
        };
    }
}
