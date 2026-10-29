using System.Globalization;

namespace Mt940Parser.Parsing
{
    /// <summary>
    /// DBS Field 61 supplementary details: <c>transactionCode?HHMMSS</c> and an optional
    /// term-deposit account after a second <c>?</c>.
    /// </summary>
    internal static class SupplementaryDetailsParser
    {
        public static void Read(
            string supplementary,
            out string dbsTransactionCode,
            out string timestamp,
            out string termDepositAccount)
        {
            dbsTransactionCode = null;
            timestamp = null;
            termDepositAccount = null;
            if (string.IsNullOrWhiteSpace(supplementary))
                return;

            var text = FirstLine(supplementary).Trim();
            var separator = text.IndexOf('?');
            if (separator != 3)
                return;

            var code = text.Substring(0, 3);
            if (!IsDigits(code))
                return;

            var rest = text.Substring(4);
            if (rest.Length < 6)
                return;

            var time = rest.Substring(0, 6);
            if (!IsDigits(time) || !IsClock(time))
                return;

            string term = null;
            if (rest.Length > 6)
            {
                if (rest[6] != '?')
                    return;
                term = rest.Substring(7).Trim();
                if (term.Length == 0)
                    term = null;
            }

            dbsTransactionCode = code;
            timestamp = time;
            termDepositAccount = term;
        }

        private static string FirstLine(string supplementary)
        {
            var normalized = supplementary.Replace("\r\n", "\n").Replace('\r', '\n');
            var newline = normalized.IndexOf('\n');
            if (newline >= 0)
                return normalized.Substring(0, newline);
            return normalized;
        }

        private static bool IsDigits(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] < '0' || text[i] > '9')
                    return false;
            }

            return text.Length > 0;
        }

        private static bool IsClock(string hhmmss)
        {
            int hour;
            int minute;
            int second;
            if (!int.TryParse(hhmmss.Substring(0, 2), NumberStyles.None, CultureInfo.InvariantCulture, out hour))
                return false;
            if (!int.TryParse(hhmmss.Substring(2, 2), NumberStyles.None, CultureInfo.InvariantCulture, out minute))
                return false;
            if (!int.TryParse(hhmmss.Substring(4, 2), NumberStyles.None, CultureInfo.InvariantCulture, out second))
                return false;
            return hour <= 23 && minute <= 59 && second <= 59;
        }
    }
}
