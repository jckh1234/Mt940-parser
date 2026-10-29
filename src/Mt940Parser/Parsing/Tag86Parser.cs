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

        // '?' is the value separator: ?CODE?value, or CODE?value at the start of the narrative.
        // Letter codes stay generic. DBS prefixes that contain digits (OTH1, OTH2, OTH3)
        // come from DbsField86Prefixes so they are recognized with the rest of that table.
        private static readonly Regex QuestionCode = BuildQuestionCodePattern();

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
            AddQuestionCodes(normalized, codes);

            foreach (var field in german.Values)
                AddPlusCodes(field, codes);

            AddPlusCodes(normalized, codes);
            return new Mt940Information(original.Trim(), transactionCode, german, codes);
        }

        private static void AddQuestionCodes(string text, Dictionary<string, string> codes)
        {
            var matches = QuestionCode.Matches(text);
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

        private static Regex BuildQuestionCodePattern()
        {
            var digitPrefixes = new List<string>();
            foreach (var prefix in DbsField86Prefixes.FieldNames.Keys)
            {
                if (!IsUpperLetters(prefix))
                    digitPrefixes.Add(Regex.Escape(prefix));
            }

            digitPrefixes.Sort(delegate(string left, string right)
            {
                return right.Length.CompareTo(left.Length);
            });

            // The '?' after the code is the value separator. A leading '?' wraps the code
            // (?CODE?value); the first code may also sit at the start (CODE?value).
            // The '?' that introduces the next code is part of that match, so it is not
            // included in the previous value.
            var body = digitPrefixes.Count == 0
                ? "[A-Z]{2,4}"
                : string.Join("|", digitPrefixes.ToArray()) + "|[A-Z]{2,4}";
            return new Regex(
                "(?:^|\\?)(" + body + ")\\?",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        private static bool IsUpperLetters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch < 'A' || ch > 'Z')
                    return false;
            }

            return true;
        }
    }
}
