using System;
using System.Globalization;
using System.IO;

namespace Mt940Parser.Sample
{
    internal static class DbsSelfTest
    {
        private const string ConnectionString = "Server=placeholder;Database=Mt940;Password=placeholder-secret;";

        public static void Run()
        {
            AssertDailyFileName();
            AssertAppendix();
            AssertField25();
            AssertSupplementary();
            AssertPdfField86();
            AssertCreditSelection();
            AssertSampleFile();
            AssertScheduler();
        }

        private static void AssertDailyFileName()
        {
            Equal("file 29 oct 2026", "MT940.D261029.txt", Mt940DailyFile.GetFileName(new DateTime(2026, 10, 29)));
            Equal("file 9 sep 2026", "MT940.D260909.txt", Mt940DailyFile.GetFileName(new DateTime(2026, 9, 9, 15, 30, 0)));
            var path = Mt940DailyFile.GetPath("shared-folder", new DateTime(2026, 10, 29));
            True("path folder", path.Contains("shared-folder"));
            True("path file", path.EndsWith("MT940.D261029.txt", StringComparison.Ordinal));
        }

        private static void AssertAppendix()
        {
            Equal("appendix rows", 48, DbsTransactionCodes.All.Count);
            Equal("fps credit", "FPS", DbsTransactionCodes.Describe("201"));
            Equal("fps credit swift", "TRF", DbsTransactionCodes.Find("201")[0].SwiftCode);
            Equal("fps credit side", "CR", DbsTransactionCodes.Find("201")[0].CreditDebit);
            Equal("outgoing tt", "OUTGOING TT", DbsTransactionCodes.Describe("508"));
            Equal("ach credit", "ACH CREDIT", DbsTransactionCodes.Describe("142"));
            Equal("ambiguous 399", "BILL PAYMENT / MISCELLANEOUS", DbsTransactionCodes.Describe("399"));
            Null("unknown code", DbsTransactionCodes.Describe("000"));
        }

        private static void AssertField25()
        {
            var accountOnly = new AccountIdentification("388888888");
            Equal("opt i number", "388888888", accountOnly.AccountNumber);
            Null("opt i bank", accountOnly.BankCode);
            Null("opt i name", accountOnly.AccountName);

            var bicAccount = new AccountIdentification("DHBKHKHH/388888888");
            Equal("opt ii bic", "DHBKHKHH", bicAccount.Bic);
            Equal("opt ii number", "388888888", bicAccount.AccountNumber);
            Null("opt ii name", bicAccount.AccountName);

            var withName = new AccountIdentification("DHBKHKHH/388888888/ABC COMPANY");
            Equal("opt iii bic", "DHBKHKHH", withName.Bic);
            Equal("opt iii number", "388888888", withName.AccountNumber);
            Equal("opt iii name", "ABC COMPANY", withName.AccountName);
            Equal("opt iii bank", "DHBKHKHH", withName.BankCode);

            var accountName = new AccountIdentification("388888888/ABC COMPANY");
            Equal("opt iv number", "388888888", accountName.AccountNumber);
            Equal("opt iv name", "ABC COMPANY", accountName.AccountName);
            Null("opt iv bank", accountName.BankCode);

            var german = new AccountIdentification("50010517/1234567890");
            Equal("blz kept", "50010517", german.BankCode);
            Equal("german account", "1234567890", german.AccountNumber);
            Null("german name", german.AccountName);
        }

        private static void AssertSupplementary()
        {
            var credit = ParseOne(@":20:A
:61:1609190919CD0,02NTRFRFBPY21//SDC690613_1
142?054836
");
            Equal("pdf credit date", new DateTime(2016, 9, 19), credit.ValueDate);
            Equal("pdf credit entry", new DateTime(2016, 9, 19), credit.EntryDate);
            Equal("pdf credit mark", DebitCreditMark.Credit, credit.Mark);
            True("pdf credit flag", credit.IsCredit);
            Equal("pdf credit funds", "D", credit.FundsCode);
            Equal("pdf credit amount", 0.02m, credit.Amount);
            Equal("pdf credit ref", "RFBPY21", credit.Reference);
            Equal("pdf credit bank", "SDC690613_1", credit.BankReference);
            Equal("pdf credit code", "142", credit.DbsTransactionCode);
            Equal("pdf credit time", "054836", credit.SupplementaryTimestamp);
            Equal("pdf credit desc", "ACH CREDIT", credit.DbsTransactionDescription);
            Null("pdf credit term", credit.TermDepositAccount);

            var debit = ParseOne(@":20:A
:61:1601190121DD38,00NTRFICMTI3//0807OT6000002734
508?085523
");
            Equal("pdf debit mark", DebitCreditMark.Debit, debit.Mark);
            False("pdf debit flag", debit.IsCredit);
            Equal("pdf debit funds", "D", debit.FundsCode);
            Equal("pdf debit amount", 38.00m, debit.Amount);
            Equal("pdf debit entry", new DateTime(2016, 1, 21), debit.EntryDate);
            Equal("pdf debit code", "508", debit.DbsTransactionCode);
            Equal("pdf debit time", "085523", debit.SupplementaryTimestamp);
            Equal("pdf debit desc", "OUTGOING TT", debit.DbsTransactionDescription);

            var deposit = ParseOne(@":20:A
:61:160119D100,00NCMTREF//BANK
601?153045?TDACC9988
");
            Equal("td code", "601", deposit.DbsTransactionCode);
            Equal("td time", "153045", deposit.SupplementaryTimestamp);
            Equal("td account", "TDACC9988", deposit.TermDepositAccount);
            Equal("td desc", "TD PLACEMENT", deposit.DbsTransactionDescription);

            var badClock = ParseOne(":20:A\n:61:160119C1,00NTRFREF\n201?256013\n");
            Equal("bad clock raw", "201?256013", badClock.SupplementaryDetails);
            Null("bad clock code", badClock.DbsTransactionCode);
            Null("bad clock time", badClock.SupplementaryTimestamp);
        }

        private static void AssertPdfField86()
        {
            var info = ParseOne(@":20:A
:61:160119D0,05NTRFREF//BANK
:86:TD?OUTGOING CHATS?BENM?CBA COMPANY?RA?HKD0,05?REMI?TESTI
NG 123456789012345. please process and debit HKD 0.01 fees?BB?
ABC BANK (HONG KONG) LIMITED
").Information;

            Equal("pdf td", "OUTGOING CHATS", info.TransactionDescription);
            Equal("pdf benm", "CBA COMPANY", info.BeneficiaryName);
            Equal("pdf ra", "HKD0,05", info.GetCodeField("RA"));
            Equal("pdf remi", "TESTING 123456789012345. please process and debit HKD 0.01 fees", info.RemittanceInformation);
            Equal("pdf purpose", "TESTING 123456789012345. please process and debit HKD 0.01 fees", info.Purpose);
            Equal("pdf bb", "ABC BANK (HONG KONG) LIMITED", info.GetCodeField("BB"));
            Equal("pdf name", "CBA COMPANY", info.CounterpartyName);
            Null("pdf booking", info.BookingText);
        }

        private static void AssertCreditSelection()
        {
            var document = Mt940Document.Parse(@"
:20:3888888888
:61:260101EC1,00NTRFEXP
:61:260101C2,50NTRFCREDIT
:86:?KREF?SEPA-ONLY
:61:260101RD3,00NTRFREV
:61:260101D4,00NTRFDEBIT
:20:9999999999
:61:260101C9,00NTRFOTHER
");
            int other;
            int nonCredit;
            var entries = DbsCreditEntry.Select(document, "3888888888", out other, out nonCredit);
            Equal("select count", 1, entries.Count);
            Equal("select other", 1, other);
            Equal("select non credit", 3, nonCredit);
            Equal("select amount", 2.50m, entries[0].Amount);
            Null("kref not cref", entries[0].CustomerReference);
            Equal("tx still has kref", "SEPA-ONLY", document.Statements[0].Transactions[1].CustomerReference);
            True("ec is not credit", !document.Statements[0].Transactions[0].IsCredit);
            True("plain c is credit", document.Statements[0].Transactions[1].IsCredit);
        }

        private static void AssertSampleFile()
        {
            var path = FindSample();
            var document = Mt940Document.Parse(File.ReadAllText(path));
            Equal("dbs statements", 2, document.Statements.Count);

            var statement = document.Statements[0];
            Equal("field 20", "3888888888", statement.DbsAccountNumber);
            Equal("field 20 alias", statement.TransactionReference, statement.DbsAccountNumber);
            Equal("field 25 bic", "DHBKHKHH", statement.Account.Bic);
            Equal("field 25 number", "3888888888", statement.Account.AccountNumber);
            Equal("field 25 name", "ABC COMPANY", statement.Account.AccountName);
            Equal("field 28 number", "00033", statement.StatementNumber.Number);
            Equal("field 28 seq", "001", statement.StatementNumber.Sequence);
            Equal("signed total", 342.50m, statement.SignedTransactionTotal);
            Equal("closing", 1342.50m, statement.ClosingBalance.SignedAmount);

            var credit = statement.Transactions[0];
            Equal("value date", new DateTime(2026, 9, 9), credit.ValueDate);
            Equal("entry date", new DateTime(2026, 9, 9), credit.EntryDate);
            True("indicator C", credit.IsCredit);
            Equal("funds D", "D", credit.FundsCode);
            Equal("amount", 297.50m, credit.Amount);
            Equal("owner ref", "-I5-4XXXKDT-BJ", credit.Reference);
            Equal("bank ref", "2609091009115RKC", credit.BankReference);
            Equal("supp raw", "201?100913", credit.SupplementaryDetails);
            Equal("supp code", "201", credit.DbsTransactionCode);
            Equal("supp time", "100913", credit.SupplementaryTimestamp);
            Equal("supp desc", "FPS", credit.DbsTransactionDescription);
            Equal("td", "FPS", credit.Information.TransactionDescription);
            Equal("pv", "10000006", credit.Information.FpsProxy);
            Equal("ordp", "MISS CHAN", credit.Information.PayerName);
            Equal("cref", "-I5-XXXXXKDT-XXXXXDG", credit.Information.CustomerReference);
            Equal("oth1", "2609091009115XXXXX159", credit.Information.TransParticular);
            Equal("purp", "CXXCH", credit.Information.PaymentPurposeCode);
            Equal("bc", "004", credit.Information.RemittanceBankBicCode);
            Null("not german 00", credit.Information.BookingText);
            Null("not german 10", credit.Information.GetGermanField("10"));
            Null("not german 26", credit.Information.GetGermanField("26"));
            Null("no remi purpose", credit.Information.Purpose);
            Equal("payer name fallback", "MISS CHAN", credit.Information.CounterpartyName);
            False("debit skipped flag", statement.Transactions[1].IsCredit);
            False("rd skipped flag", statement.Transactions[2].IsCredit);

            int other;
            int nonCredit;
            var entries = DbsCreditEntry.Select(document, "3888888888", out other, out nonCredit);
            Equal("rows", 2, entries.Count);
            Equal("skipped other", 1, other);
            Equal("skipped non credit", 2, nonCredit);
            Equal("row amount", 297.50m, entries[0].Amount);
            Equal("row time", "100913", entries[0].SupplementaryTimestamp);
            Equal("row td", "FPS", entries[0].TransactionDescription);
            Equal("row pv", "10000006", entries[0].FpsProxy);
            Equal("row ordp", "MISS CHAN", entries[0].PayerName);
            Equal("row cref", "-I5-XXXXXKDT-XXXXXDG", entries[0].CustomerReference);
            Equal("row oth1", "2609091009115XXXXX159", entries[0].TransParticular);
            Equal("row purp", "CXXCH", entries[0].PaymentPurposeCode);
            Equal("row bc", "004", entries[0].RemittanceBankBicCode);
            Equal("row 2 cref", "BILL1234", entries[1].CustomerReference);
            Equal("row 2 amount", 50.00m, entries[1].Amount);
            Equal("row 2 bc", "012", entries[1].RemittanceBankBicCode);
            Equal("other account cref", "OTHERCREF", document.Statements[1].Transactions[0].CustomerReference);
        }

        private static void AssertScheduler()
        {
            var folder = Path.Combine(Path.GetTempPath(), "mt940-scheduler-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var missingFolder = DailyStatementProcessor.Process(
                    Path.Combine(folder, "missing"),
                    "3888888888",
                    new DateTime(2026, 10, 29),
                    ConnectionString);
                Equal("missing folder", DailyStatementProcessor.FileNotFoundExitCode, missingFolder.ExitCode);
                True("missing folder message", missingFolder.Messages[0].Contains("not found"));

                var date = new DateTime(2026, 10, 29);
                var missingFile = DailyStatementProcessor.Process(folder, "3888888888", date, ConnectionString);
                Equal("missing file", DailyStatementProcessor.FileNotFoundExitCode, missingFile.ExitCode);
                True("missing name", missingFile.FilePath.EndsWith("MT940.D261029.txt", StringComparison.Ordinal));
                True("missing message", Join(missingFile.Messages).Contains("MT940.D261029.txt"));

                var badPath = Path.Combine(folder, Mt940DailyFile.GetFileName(date));
                File.WriteAllText(badPath, ":20:BAD\n:61:NOTVALID\n");
                var bad = DailyStatementProcessor.Process(folder, "3888888888", date, ConnectionString);
                Equal("parse exit", DailyStatementProcessor.ParseErrorExitCode, bad.ExitCode);
                Equal("parse inserts", 0, bad.InsertScripts.Count);
                True("parse tag", Join(bad.Messages).Contains(":61:"));

                File.WriteAllText(badPath, File.ReadAllText(FindSample()));
                var result = DailyStatementProcessor.Process(folder, "3888888888", date, ConnectionString);
                Equal("run exit", DailyStatementProcessor.SuccessExitCode, result.ExitCode);
                Equal("run rows", 2, result.Entries.Count);
                Equal("run scripts", 2, result.InsertScripts.Count);
                var script = result.InsertScripts[0];
                True("script amount", script.Contains("297.50"));
                True("script time", script.Contains("N'100913'"));
                True("script td", script.Contains("N'FPS'"));
                True("script pv", script.Contains("N'10000006'"));
                True("script ordp", script.Contains("N'MISS CHAN'"));
                True("script cref", script.Contains("N'-I5-XXXXXKDT-XXXXXDG'"));
                True("script oth1", script.Contains("N'2609091009115XXXXX159'"));
                True("script purp", script.Contains("N'CXXCH'"));
                True("script bc", script.Contains("N'004'"));
                True("script account", script.Contains("N'3888888888'"));
                False("script secret", script.Contains("placeholder-secret"));
                False("messages secret", Join(result.Messages).Contains("placeholder-secret"));
                False("other cref inserted", script.Contains("OTHERCREF") || result.InsertScripts[1].Contains("OTHERCREF"));
                False("debit cref inserted", Join(result.InsertScripts).Contains("DEBITCREF"));
                True("pseudo note", Join(result.Messages).Contains("No connection was opened"));

                var wrongAccount = DailyStatementProcessor.Process(folder, "0000000000", date, ConnectionString);
                Equal("wrong account exit", DailyStatementProcessor.SuccessExitCode, wrongAccount.ExitCode);
                Equal("wrong account rows", 0, wrongAccount.Entries.Count);
                True("wrong account message", Join(wrongAccount.Messages).Contains("No statement with Field 20 account"));

                File.WriteAllText(badPath, ":20:3888888888\n:61:260909C10,00NTRFREF\n:86:TD?FPS?ORDP?MISS CHAN?CREF?ABC\n");
                var partial = DailyStatementProcessor.Process(folder, "3888888888", date, ConnectionString);
                Equal("partial exit", DailyStatementProcessor.SuccessExitCode, partial.ExitCode);
                Equal("partial rows", 1, partial.Entries.Count);
                True("partial warning", Join(partial.Messages).Contains("missing Payment Purpose Code (PURP)"));
                True("partial still inserted", partial.InsertScripts[0].Contains("N'ABC'"));
                True("partial null purp", partial.InsertScripts[0].Contains("NULL"));

                var noConnection = DailyStatementProcessor.Process(folder, "3888888888", date, "  ");
                Equal("no connection", DailyStatementProcessor.InvalidArgumentsExitCode, noConnection.ExitCode);
                Equal("no connection scripts", 0, noConnection.InsertScripts.Count);
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        private static Mt940Transaction ParseOne(string content)
        {
            return Mt940Document.Parse(content).Statements[0].Transactions[0];
        }

        private static string FindSample()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "samples", "MT940.D260909.txt");
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }

            throw new FileNotFoundException("Could not find samples/MT940.D260909.txt.");
        }

        private static string Join(System.Collections.Generic.IReadOnlyList<string> messages)
        {
            return string.Join("\n", messages);
        }

        private static void Equal(string name, object expected, object actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    name + " expected [" + Format(expected) + "] but was [" + Format(actual) + "].");
            }
        }

        private static void Null(string name, object actual)
        {
            if (actual != null)
                throw new InvalidOperationException(name + " expected null but was [" + Format(actual) + "].");
        }

        private static void True(string name, bool condition)
        {
            if (!condition)
                throw new InvalidOperationException(name + " expected true.");
        }

        private static void False(string name, bool condition)
        {
            if (condition)
                throw new InvalidOperationException(name + " expected false.");
        }

        private static string Format(object value)
        {
            if (value == null)
                return "null";
            if (value is DateTime)
                return ((DateTime)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (value is decimal)
                return ((decimal)value).ToString(CultureInfo.InvariantCulture);
            return value.ToString();
        }
    }
}
