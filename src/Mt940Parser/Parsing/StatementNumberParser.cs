using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mt940Parser.Parsing
{
    internal static class StatementNumberParser
    {
        private static readonly Regex Pattern = new Regex(
            @"^(?<number>\d{1,5})(?:/(?<sequence>\d{1,5}))?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static StatementNumber Parse(
            string content,
            string tag,
            int lineNumber,
            IList<Mt940ParseException> errors)
        {
            var sourceTag = ":" + tag + ":";
            var raw = content == null ? string.Empty : content.Replace("\n", string.Empty).Trim();
            if (raw.Length == 0)
            {
                errors.Add(new Mt940ParseException("Statement number is empty.", lineNumber, sourceTag));
                return new StatementNumber(raw, null, null, sourceTag);
            }

            var match = Pattern.Match(raw);
            if (!match.Success)
            {
                errors.Add(new Mt940ParseException(
                    "Statement number must be n[/n], for example 00001/001.",
                    lineNumber,
                    sourceTag));
                return new StatementNumber(raw, null, null, sourceTag);
            }

            var sequence = match.Groups["sequence"].Success ? match.Groups["sequence"].Value : null;
            return new StatementNumber(raw, match.Groups["number"].Value, sequence, sourceTag);
        }
    }
}
