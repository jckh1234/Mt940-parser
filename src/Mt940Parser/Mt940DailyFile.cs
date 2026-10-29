using System;
using System.Globalization;
using System.IO;

namespace Mt940Parser
{
    /// <summary>
    /// Daily statement file name <c>MT940.DyyMMdd.txt</c>.
    /// 29 October 2026 is <c>MT940.D261029.txt</c>.
    /// </summary>
    public static class Mt940DailyFile
    {
        /// <summary>File name for <paramref name="statementDate"/>, ignoring the time of day.</summary>
        public static string GetFileName(DateTime statementDate)
        {
            return "MT940.D" + statementDate.ToString("yyMMdd", CultureInfo.InvariantCulture) + ".txt";
        }

        /// <summary>Full path of the daily file inside <paramref name="sharedFolder"/>.</summary>
        public static string GetPath(string sharedFolder, DateTime statementDate)
        {
            if (string.IsNullOrWhiteSpace(sharedFolder))
                throw new ArgumentException("Shared folder is required.", nameof(sharedFolder));
            return Path.Combine(sharedFolder, GetFileName(statementDate));
        }
    }
}
