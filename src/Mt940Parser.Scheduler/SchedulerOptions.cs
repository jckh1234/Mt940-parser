using System;
using System.Globalization;

namespace Mt940Parser.Scheduler
{
    internal sealed class SchedulerOptions
    {
        public const string Usage =
            "Usage:\n"
            + "  Mt940Parser.Scheduler --folder <shared-folder> --account <field-20-account> --connection <connection-string> [--date yyyy-MM-dd]\n"
            + "\n"
            + "Reads MT940.DyyMMdd.txt from the shared folder (29 Oct 2026 is MT940.D261029.txt).\n"
            + "Keeps statements whose Field 20 equals the account. Inserts Field 61 lines whose\n"
            + "debit/credit indicator is C. The database insert is pseudo-code and does not open a connection.\n"
            + "\n"
            + "Exit codes: 0 success, 1 invalid arguments, 2 file not found, 3 parse error, 4 read error.";

        private SchedulerOptions(bool help, string sharedFolder, string accountNumber, DateTime statementDate, string connectionString)
        {
            Help = help;
            SharedFolder = sharedFolder;
            AccountNumber = accountNumber;
            StatementDate = statementDate.Date;
            ConnectionString = connectionString;
        }

        public bool Help { get; }

        public string SharedFolder { get; }

        public string AccountNumber { get; }

        public DateTime StatementDate { get; }

        public string ConnectionString { get; }

        public static bool TryParse(string[] args, out SchedulerOptions options, out string error)
        {
            options = null;
            error = null;
            if (args == null || args.Length == 0)
            {
                error = "Arguments are required.";
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (IsHelp(args[i]))
                {
                    options = new SchedulerOptions(true, null, null, DateTime.Today, null);
                    return true;
                }
            }

            string folder = null;
            string account = null;
            string connection = null;
            DateTime? date = null;
            for (int i = 0; i < args.Length; i++)
            {
                string name;
                string value;
                if (!TryReadOption(args, ref i, out name, out value, out error))
                    return false;

                if (name == "folder")
                {
                    if (folder != null)
                    {
                        error = "Duplicate --folder.";
                        return false;
                    }

                    folder = value;
                }
                else if (name == "account")
                {
                    if (account != null)
                    {
                        error = "Duplicate --account.";
                        return false;
                    }

                    account = value;
                }
                else if (name == "connection")
                {
                    if (connection != null)
                    {
                        error = "Duplicate --connection.";
                        return false;
                    }

                    connection = value;
                }
                else if (name == "date")
                {
                    if (date.HasValue)
                    {
                        error = "Duplicate --date.";
                        return false;
                    }

                    DateTime parsed;
                    if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                    {
                        error = "Date must be yyyy-MM-dd.";
                        return false;
                    }

                    date = parsed;
                }
                else
                {
                    error = "Unknown option '--" + name + "'.";
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(folder))
            {
                error = "Shared folder is required (--folder).";
                return false;
            }

            if (string.IsNullOrWhiteSpace(account))
            {
                error = "Field 20 account is required (--account).";
                return false;
            }

            if (string.IsNullOrWhiteSpace(connection))
            {
                error = "Connection string is required (--connection).";
                return false;
            }

            options = new SchedulerOptions(false, folder.Trim(), account.Trim(), date ?? DateTime.Today, connection);
            return true;
        }

        private static bool IsHelp(string arg)
        {
            return string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase)
                || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)
                || string.Equals(arg, "-help", StringComparison.OrdinalIgnoreCase)
                || string.Equals(arg, "/?", StringComparison.Ordinal)
                || string.Equals(arg, "/help", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryReadOption(string[] args, ref int index, out string name, out string value, out string error)
        {
            name = null;
            value = null;
            error = null;
            var arg = args[index] ?? string.Empty;
            var token = arg;
            if (token.StartsWith("--", StringComparison.Ordinal))
                token = token.Substring(2);
            else if (token.StartsWith("-", StringComparison.Ordinal) || token.StartsWith("/", StringComparison.Ordinal))
                token = token.Substring(1);
            else
            {
                error = "Unexpected argument '" + arg + "'.";
                return false;
            }

            var equals = token.IndexOf('=');
            if (equals >= 0)
            {
                name = token.Substring(0, equals).ToLowerInvariant();
                value = token.Substring(equals + 1);
            }
            else
            {
                name = token.ToLowerInvariant();
                if (index + 1 >= args.Length)
                {
                    error = "Missing value for --" + name + ".";
                    return false;
                }

                index++;
                value = args[index];
            }

            if (name.Length == 0)
            {
                error = "Missing option name.";
                return false;
            }

            return true;
        }
    }
}
