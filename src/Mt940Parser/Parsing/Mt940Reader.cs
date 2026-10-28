using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Mt940Parser.Parsing
{
    internal static class Mt940Reader
    {
        private static readonly Regex TagPattern = new Regex(
            @"^:([0-9]{2}[A-Za-z]?):(.*)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static Mt940Document Read(string content, IList<Mt940ParseException> errors)
        {
            var statements = new List<Mt940Statement>();
            if (string.IsNullOrWhiteSpace(content))
            {
                errors.Add(new Mt940ParseException("Content is empty.", 0, null));
                return new Mt940Document(statements);
            }

            content = content.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\0", string.Empty);
            if (content.Length > 0 && content[0] == '\uFEFF')
                content = content.Substring(1);

            var lines = content.Split('\n');
            var builder = new StatementBuilder(errors);
            string currentTag = null;
            var currentTagLine = 0;
            var buffer = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                var lineNumber = i + 1;
                var line = lines[i].TrimEnd();
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                    continue;

                if (IsBoundary(trimmed))
                {
                    FlushTag();
                    builder.Complete(statements);
                    continue;
                }

                if (trimmed[0] == '{')
                    continue;

                var match = TagPattern.Match(trimmed);
                if (match.Success)
                {
                    FlushTag();
                    var tag = match.Groups[1].Value.ToUpperInvariant();
                    if (tag == "20" && builder.HasContent)
                        builder.Complete(statements);

                    currentTag = tag;
                    currentTagLine = lineNumber;
                    buffer.Clear();
                    buffer.Append(match.Groups[2].Value.Trim());
                    continue;
                }

                if (currentTag == null)
                {
                    errors.Add(new Mt940ParseException("Unexpected text outside a tag.", lineNumber, null));
                    continue;
                }

                if (buffer.Length > 0)
                    buffer.Append('\n');
                buffer.Append(line);
            }

            FlushTag();
            builder.Complete(statements);

            if (statements.Count == 0 && errors.Count == 0)
                errors.Add(new Mt940ParseException("No MT940 statement was found.", 1, null));

            return new Mt940Document(statements);

            void FlushTag()
            {
                if (currentTag == null)
                    return;
                builder.Apply(currentTag, buffer.ToString(), currentTagLine);
                currentTag = null;
                buffer.Clear();
            }
        }

        private static bool IsBoundary(string trimmed)
        {
            return trimmed == "-"
                || trimmed == "$"
                || trimmed.StartsWith("-}", System.StringComparison.Ordinal);
        }
    }
}
