using System;
using System.Collections.Generic;

namespace Liuruan
{
    internal static class R3Data
    {
        internal static T[] Required<T>(T[] values, string field)
        {
            if (values == null || values.Length == 0) throw new ArgumentException(field + " 不可空白。");
            foreach (var value in values) if (value == null) throw new ArgumentException(field + " 含空項目。");
            return values;
        }
        internal static string Text(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(field + " 不可空白。");
            return value;
        }
        internal static void Unique<T>(HashSet<T> seen, T value, string field)
        { if (!seen.Add(value)) throw new ArgumentException(field + " 重複。"); }
        internal static void Time(float dt)
        {
            HungerCore.RequireFinite(dt, nameof(dt));
            if (dt < 0) throw new ArgumentOutOfRangeException(nameof(dt));
        }
        internal static void Act(int act)
        { if (act < 1 || act > 5) throw new ArgumentException("幕次必須為 1–5。"); }
    }
}
