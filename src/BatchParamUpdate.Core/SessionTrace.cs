using System.Globalization;

namespace BatchParamUpdate.Core;

public static class SessionTrace
{
    // ponytail: TSV after the logger's ts/level. Do not log WPF CanExecute polls.
    public static string Line(string layer, string surface, string evt, params (string Key, object? Value)[] facts)
    {
        if (facts.Length == 0)
            return $"{layer}\t{surface}\t{evt}";

        return $"{layer}\t{surface}\t{evt}\t{string.Join(" ", facts.Select(f => $"{f.Key}={Render(f.Value)}"))}";
    }

    private static string Render(object? value)
    {
        var text = value switch
        {
            null => "-",
            bool flag => flag ? "true" : "false",
            string s => s.Length == 0 ? "''" : s,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "-",
            _ => value.ToString() ?? "-"
        };
        return NeedsQuote(text) ? $"'{text.Replace("'", "''", StringComparison.Ordinal)}'" : text;
    }

    private static bool NeedsQuote(string text)
        => text.AsSpan().IndexOfAny(" \t='\n\r") >= 0;
}
