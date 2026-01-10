using CurrencyWatchdog.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CurrencyWatchdog.Utility;

public static class Extensions {
    extension<T>(T self) where T : struct, Enum {
        public string GetDisplayName() {
            if (Enum.GetName(self) is { } name
                && typeof(T).GetField(name) is { } field
                && field.GetCustomAttribute<DisplayAttribute>() is { } displayAttribute
                && displayAttribute.GetName() is { } displayName) {
                return displayName;
            }
            return self.ToString();
        }
    }

    extension(SubjectExpression self) {
        public string GetDisplayName() {
            return self switch {
                SubjectExpression.Constant constant => constant.Value.ToString(Utils.DecimalDisplayFormat),
                SubjectExpression.Metric metric => metric.Type.GetDisplayName(),
                _ => "???",
            };
        }
    }

    extension<T>(IList<T> self) {
        public void Move(int fromIndex, int toIndex) {
            var item = self[fromIndex];
            self.RemoveAt(fromIndex);
            self.Insert(toIndex, item);
        }

        public void Move(int fromIndex, int toIndex, ref int trackedIndex) {
            self.Move(fromIndex, toIndex);

            if (trackedIndex >= 0)
                trackedIndex = AdjustIndexAfterMove(trackedIndex, fromIndex, toIndex);
        }
    }

    private static int AdjustIndexAfterMove(int trackedIndex, int fromIndex, int toIndex) {
        if (trackedIndex == fromIndex)
            return toIndex;

        if (fromIndex < toIndex && trackedIndex > fromIndex && trackedIndex <= toIndex)
            return trackedIndex - 1;

        if (fromIndex > toIndex && trackedIndex >= toIndex && trackedIndex < fromIndex)
            return trackedIndex + 1;

        return trackedIndex;
    }
}
