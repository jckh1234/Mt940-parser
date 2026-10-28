using System;
using System.Globalization;

namespace Mt940Parser.Parsing
{
    internal static class SwiftAmount
    {
        public static bool TryParse(string raw, out decimal amount)
        {
            amount = 0m;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var text = raw.Trim();
            var commas = Count(text, ',');
            var dots = Count(text, '.');

            if (commas > 0 && dots > 0)
            {
                if (text.LastIndexOf(',') > text.LastIndexOf('.'))
                    text = text.Replace(".", string.Empty).Replace(',', '.');
                else
                    text = text.Replace(",", string.Empty);
            }
            else if (commas > 1)
            {
                return false;
            }
            else if (commas == 1)
            {
                text = text.Replace(',', '.');
            }
            else if (dots > 1)
            {
                text = text.Replace(".", string.Empty);
            }

            if (text.EndsWith(".", StringComparison.Ordinal))
                text = text.Substring(0, text.Length - 1);

            if (text.Length == 0)
                return false;

            decimal parsed;
            if (!decimal.TryParse(
                text,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out parsed))
            {
                return false;
            }

            if (parsed < 0m)
                return false;

            amount = parsed;
            return true;
        }

        private static int Count(string text, char value)
        {
            var count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == value)
                    count++;
            }

            return count;
        }
    }
}
