using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Mt940Parser
{
    /// <summary>
    /// Reads one daily MT940 file, keeps Field 20 for a single account, and records a pseudo insert
    /// for each Field 61 credit (indicator C).
    /// </summary>
    public static class DailyStatementProcessor
    {
        /// <summary>The run finished. Zero credit lines is still success.</summary>
        public const int SuccessExitCode = 0;

        /// <summary>Folder, account, or connection string was missing or invalid.</summary>
        public const int InvalidArgumentsExitCode = 1;

        /// <summary>The shared folder or today's statement file was not found.</summary>
        public const int FileNotFoundExitCode = 2;

        /// <summary>The file was found but is not valid MT940. Nothing is inserted.</summary>
        public const int ParseErrorExitCode = 3;

        /// <summary>The file could not be read.</summary>
        public const int IoErrorExitCode = 4;

        /// <summary>
        /// Loads <c>MT940.DyyMMdd.txt</c> from <paramref name="sharedFolder"/> and builds one pseudo insert
        /// per credit line for <paramref name="field20AccountNumber"/>.
        /// </summary>
        public static DailyStatementResult Process(
            string sharedFolder,
            string field20AccountNumber,
            DateTime statementDate,
            string connectionString)
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(sharedFolder))
            {
                messages.Add("Shared folder is required.");
                return DailyStatementResult.Failed(InvalidArgumentsExitCode, null, messages);
            }

            if (string.IsNullOrWhiteSpace(field20AccountNumber))
            {
                messages.Add("Field 20 account number is required.");
                return DailyStatementResult.Failed(InvalidArgumentsExitCode, null, messages);
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                messages.Add("Connection string is required.");
                return DailyStatementResult.Failed(InvalidArgumentsExitCode, null, messages);
            }

            string path;
            try
            {
                path = Mt940DailyFile.GetPath(sharedFolder.Trim(), statementDate);
            }
            catch (ArgumentException ex)
            {
                messages.Add(ex.Message);
                return DailyStatementResult.Failed(InvalidArgumentsExitCode, null, messages);
            }

            if (!Directory.Exists(sharedFolder.Trim()))
            {
                messages.Add("Shared folder was not found: " + sharedFolder.Trim());
                return DailyStatementResult.Failed(FileNotFoundExitCode, path, messages);
            }

            if (!File.Exists(path))
            {
                messages.Add("Statement file was not found: " + path);
                return DailyStatementResult.Failed(FileNotFoundExitCode, path, messages);
            }

            string content;
            try
            {
                content = ReadSharedFile(path);
            }
            catch (IOException ex)
            {
                messages.Add("Could not read statement file: " + ex.Message);
                return DailyStatementResult.Failed(IoErrorExitCode, path, messages);
            }
            catch (UnauthorizedAccessException ex)
            {
                messages.Add("Could not read statement file: " + ex.Message);
                return DailyStatementResult.Failed(IoErrorExitCode, path, messages);
            }

            Mt940Document document;
            IList<Mt940ParseException> errors;
            if (!Mt940Document.TryParse(content, out document, out errors))
            {
                messages.Add("Statement file was not parsed. No rows were inserted.");
                for (int i = 0; i < errors.Count; i++)
                    messages.Add(errors[i].Message);
                return DailyStatementResult.Failed(ParseErrorExitCode, path, messages);
            }

            int skippedOtherStatements;
            int skippedNonCreditLines;
            var entries = DbsCreditEntry.Select(
                document,
                field20AccountNumber,
                out skippedOtherStatements,
                out skippedNonCreditLines);

            var scripts = new List<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                WarnIfMissing(messages, entries[i]);
                var script = CreditStatementStore.Insert(connectionString, entries[i]);
                scripts.Add(script);
                messages.Add(FormatInserted(entries[i]));
            }

            messages.Add(string.Format(
                CultureInfo.InvariantCulture,
                "Credit lines inserted: {0}. Non-credit lines skipped: {1}. Other statements skipped: {2}.",
                entries.Count,
                skippedNonCreditLines,
                skippedOtherStatements));
            messages.Add("Database insert is pseudo-code only. No connection was opened.");
            if (entries.Count == 0 && skippedOtherStatements == document.Statements.Count)
            {
                messages.Add(
                    "No statement with Field 20 account '" + field20AccountNumber.Trim() + "' was found.");
            }

            return new DailyStatementResult(SuccessExitCode, path, messages, entries, scripts);
        }

        private static string ReadSharedFile(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
        }

        private static void WarnIfMissing(IList<string> messages, DbsCreditEntry entry)
        {
            Warn(messages, entry, "Supplementary Timestamp", entry.SupplementaryTimestamp);
            Warn(messages, entry, "Transaction Description (TD)", entry.TransactionDescription);
            Warn(messages, entry, "FPS Proxy (PV)", entry.FpsProxy);
            Warn(messages, entry, "Payer Name (ORDP)", entry.PayerName);
            Warn(messages, entry, "Customer Reference (CREF)", entry.CustomerReference);
            Warn(messages, entry, "TRANS PARTICULAR (OTH1)", entry.TransParticular);
            Warn(messages, entry, "Payment Purpose Code (PURP)", entry.PaymentPurposeCode);
            Warn(messages, entry, "Remittance Bank Bic Code (BC)", entry.RemittanceBankBicCode);
        }

        private static void Warn(IList<string> messages, DbsCreditEntry entry, string label, string value)
        {
            if (!string.IsNullOrEmpty(value))
                return;
            messages.Add(string.Format(
                CultureInfo.InvariantCulture,
                "Warning: credit {0:yyyy-MM-dd} amount {1:0.00} is missing {2}.",
                entry.ValueDate,
                entry.Amount,
                label));
        }

        private static string FormatInserted(DbsCreditEntry entry)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Inserted credit {0:yyyy-MM-dd} {1:0.00} CREF={2} (pseudo-code only; no database connection opened).",
                entry.ValueDate,
                entry.Amount,
                entry.CustomerReference ?? "");
        }
    }

    /// <summary>
    /// Outcome of <see cref="DailyStatementProcessor.Process"/>.
    /// </summary>
    public sealed class DailyStatementResult
    {
        /// <summary>
        /// Creates a result. Lists are copied.
        /// </summary>
        public DailyStatementResult(
            int exitCode,
            string filePath,
            IEnumerable<string> messages,
            IEnumerable<DbsCreditEntry> entries,
            IEnumerable<string> insertScripts)
        {
            ExitCode = exitCode;
            FilePath = filePath;
            Messages = CopyStrings(messages);
            Entries = CopyEntries(entries);
            InsertScripts = CopyStrings(insertScripts);
        }

        /// <summary>Process exit code. Zero is success.</summary>
        public int ExitCode { get; }

        /// <summary>Statement path that was resolved, or null when the folder argument was rejected.</summary>
        public string FilePath { get; }

        /// <summary>Operator messages, in order.</summary>
        public IReadOnlyList<string> Messages { get; }

        /// <summary>Credit rows that were handed to the pseudo insert. Empty when the run did not insert.</summary>
        public IReadOnlyList<DbsCreditEntry> Entries { get; }

        /// <summary>Pseudo INSERT scripts, one per credit row.</summary>
        public IReadOnlyList<string> InsertScripts { get; }

        internal static DailyStatementResult Failed(int exitCode, string filePath, IList<string> messages)
        {
            return new DailyStatementResult(exitCode, filePath, messages, new DbsCreditEntry[0], new string[0]);
        }

        private static IReadOnlyList<string> CopyStrings(IEnumerable<string> values)
        {
            var copy = new List<string>();
            if (values != null)
            {
                foreach (var value in values)
                {
                    if (value != null)
                        copy.Add(value);
                }
            }

            return copy.AsReadOnly();
        }

        private static IReadOnlyList<DbsCreditEntry> CopyEntries(IEnumerable<DbsCreditEntry> values)
        {
            var copy = new List<DbsCreditEntry>();
            if (values != null)
            {
                foreach (var value in values)
                {
                    if (value != null)
                        copy.Add(value);
                }
            }

            return copy.AsReadOnly();
        }
    }
}
