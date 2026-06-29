using System;
using System.Globalization;
using UnityEngine;

namespace CarFactoryIdle.Core
{
    /// <summary>Central, display-only number formatter. Logic always keeps raw values; only labels
    /// route through here. Honors the player's "short notation" preference (Settings toggle):
    /// SHORT → abbreviated (1.2M), FULL → comma-grouped (1,200,000). Abbreviation rule: values below
    /// 1000 are shown plain; from there K/M/B/T with at most one decimal, trailing ".0" dropped
    /// (12K, not 12.0K). Negatives and zero are handled.</summary>
    public static class NumberFormat
    {
        // Same PlayerPrefs key SettingsView writes, kept local so Core has no dependency on UI.
        private const string KNotation = "cfi_notation_short";

        private static readonly string[] Suffixes = { "K", "M", "B", "T" };

        /// <summary>True when the player prefers abbreviated numbers (mirrors SettingsView.ShortNotation).</summary>
        public static bool ShortNotation => PlayerPrefs.GetInt(KNotation, 0) == 1;

        /// <summary>A plain count, formatted per the player's notation preference.</summary>
        public static string Format(long value) => ShortNotation ? Abbrev(value) : Full(value);

        /// <summary>A currency value (prefixed with $), formatted per the player's preference.</summary>
        public static string Currency(long value) => "$" + Format(value);

        /// <summary>Comma-grouped full form, e.g. 1,200,000.</summary>
        public static string Full(long value) => value.ToString("n0", CultureInfo.InvariantCulture);

        /// <summary>Abbreviated form: &lt;1000 plain; then K/M/B/T with up to one decimal (trailing
        /// .0 dropped). Truncates rather than rounds so a value never visually rolls past its tier.</summary>
        public static string Abbrev(long value)
        {
            if (value == long.MinValue) return Full(value); // can't negate safely; fall back
            if (value < 0) return "-" + Abbrev(-value);
            if (value < 1000) return value.ToString(CultureInfo.InvariantCulture);

            double v = value;
            int tier = -1;
            while (v >= 1000d && tier < Suffixes.Length - 1) { v /= 1000d; tier++; }

            double truncated = Math.Floor(v * 10d) / 10d; // 1 decimal, truncated
            return truncated.ToString("0.#", CultureInfo.InvariantCulture) + Suffixes[tier];
        }
    }
}
