using System.Text.RegularExpressions;

namespace Mt940Parser.Parsing
{
    internal static class AccountTokens
    {
        private static readonly Regex IbanPattern = new Regex(
            @"^[A-Z]{2}\d{2}[A-Z0-9]{11,30}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex BicPattern = new Regex(
            @"^[A-Z]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string Compact(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var chars = new char[value.Length];
            var length = 0;
            for (int i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (char.IsWhiteSpace(ch))
                    continue;
                chars[length++] = char.ToUpperInvariant(ch);
            }

            if (length == 0)
                return null;
            return new string(chars, 0, length);
        }

        public static bool IsIban(string compact)
        {
            return compact != null && IbanPattern.IsMatch(compact);
        }

        public static bool IsBic(string compact)
        {
            return compact != null && BicPattern.IsMatch(compact);
        }

        public static string NormalizeIban(string value)
        {
            var compact = Compact(value);
            return IsIban(compact) ? compact : null;
        }

        public static string NormalizeBic(string value)
        {
            var compact = Compact(value);
            return IsBic(compact) ? compact : null;
        }
    }
}
