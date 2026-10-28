using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mt940Parser.Parsing
{
    internal static class Tag86Parser
    {
        private static readonly Regex GermanSeparator = new Regex(
            @"\?(\d{2})",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex SlashCode = new Regex(
            @"/([A-Z]{2,4})/",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex PlusCode = new Regex(
            @"(EREF|KREF|MREF|CRED|DEBT|SVWZ|ABWA|ABWE|PURP)\+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static Mt940Information Parse(string raw)
        {
            var original = raw ?? string.Empty;
            var normalized = original.Replace("\r", string.Empty).Replace("\n", string.Empty);
            var german = new Dictionary<string, string>(StringComparer.Ordinal);
            string transactionCode = null;

            var separators = GermanSeparator.Matches(normalized);
            if (separators.Count > 0)
            {
                var head = normalized.Substring(0, separators[0].Index).Trim();
                if (head.Length == 3 && IsDigits(head))
                    transactionCode = head;

                for (int i = 0; i < separators.Count; i++)
                {
                    var code = separators[i].Groups[1].Value;
                    var start = separators[i].Index + separators[i].Length;
                    var end = i + 1 < separators.Count ? separators[i + 1].Index : normalized.Length;
                    var value = normalized.Substring(start, end - start).Trim();
                    if (value.Length == 0)
                        continue;

                    string existing;
                    if (german.TryGetValue(code, out existing))
                        german[code] = existing + value;
                    else
                        german[code] = value;
                }
            }

            var codes = new Dictionary<string, string>(StringComparer.Ordinal);
            AddSlashCodes(normalized, codes);

            foreach (var field in german.Values)
                AddPlusCodes(field, codes);

            AddPlusCodes(normalized, codes);
            return new Mt940Information(original.Trim(), transactionCode, german, codes);
        }

        private static void AddSlashCodes(string text, Dictionary<string, string> codes)
        {
            var matches = SlashCode.Matches(text);
            for (int i = 0; i < matches.Count; i++)
            {
                var code = matches[i].Groups[1].Value;
                var start = matches[i].Index + matches[i].Length;
                var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
                AddIfMissing(codes, code, text.Substring(start, end - start).Trim());
            }
        }

        private static void AddPlusCodes(string text, Dictionary<string, string> codes)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var matches = PlusCode.Matches(text);
            for (int i = 0; i < matches.Count; i++)
            {
                var index = matches[i].Index;
                if (index > 0 && char.IsLetter(text[index - 1]))
                    continue;

                var code = matches[i].Groups[1].Value;
                var start = index + matches[i].Length;
                var end = text.Length;
                for (int j = i + 1; j < matches.Count; j++)
                {
                    var next = matches[j].Index;
                    if (next > 0 && char.IsLetter(text[next - 1]))
                        continue;
                    end = next;
                    break;
                }

                var question = text.IndexOf('?', start);
                if (question >= 0 && question < end)
                    end = question;

                AddIfMissing(codes, code, text.Substring(start, end - start).Trim());
            }
        }

        private static void AddIfMissing(Dictionary<string, string> codes, string code, string value)
        {
            if (string.IsNullOrEmpty(value) || codes.ContainsKey(code))
                return;
            codes.Add(code, value);
        }

        private static bool IsDigits(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]))
                    return false;
            }

            return true;
        }
    }
}
