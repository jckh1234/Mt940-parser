using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Mt940Parser.Sample
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var bundled = FindBundledSample();
                if (args.Length > 0)
                {
                    var path = args[0];
                    var custom = Mt940Document.Parse(File.ReadAllText(path, Encoding.UTF8));
                    Print(custom, path);
                    Console.WriteLine();
                }

                var text = File.ReadAllText(bundled, Encoding.UTF8);
                var document = Mt940Document.Parse(text);
                if (args.Length == 0)
                    Print(document, bundled);

                SelfTest.Run(document, text, bundled);
                Console.WriteLine();
                Console.WriteLine("All self-tests passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static void Print(Mt940Document document, string path)
        {
            Console.WriteLine("File: " + path);
            Console.WriteLine("Statements: " + document.Statements.Count.ToString(CultureInfo.InvariantCulture));
            for (int s = 0; s < document.Statements.Count; s++)
            {
                var statement = document.Statements[s];
                Console.WriteLine();
                Console.WriteLine(
                    "Statement " + (s + 1).ToString(CultureInfo.InvariantCulture)
                    + ": " + (statement.TransactionReference ?? "(no :20:)"));
                if (statement.RelatedReference != null)
                    Console.WriteLine("  Related: " + statement.RelatedReference);
                if (statement.Account != null)
                    Console.WriteLine("  Account: " + statement.Account.Raw);
                if (statement.StatementNumber != null)
                    Console.WriteLine("  Number:  " + statement.StatementNumber.Raw + " (" + statement.StatementNumber.SourceTag + ")");
                PrintBalance("Opening", statement.OpeningBalance);
                for (int t = 0; t < statement.Transactions.Count; t++)
                {
                    var tx = statement.Transactions[t];
                    var purpose = tx.Information == null ? null : tx.Information.Purpose;
                    Console.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "  {0:yyyy-MM-dd} {1,-2} {2,12:0.00} {3} {4} CustomerReference={5}",
                        tx.ValueDate,
                        tx.Mark.ToSwiftCode(),
                        tx.SignedAmount,
                        tx.Reference ?? "",
                        purpose ?? "",
                        tx.CustomerReference ?? ""));
                }

                PrintBalance("Closing", statement.ClosingBalance);
                PrintBalance("Available", statement.ClosingAvailableBalance);
                for (int i = 0; i < statement.ForwardAvailableBalances.Count; i++)
                    PrintBalance("Forward", statement.ForwardAvailableBalances[i]);
                if (statement.Information != null && statement.Information.Purpose != null)
                    Console.WriteLine("  Info: " + statement.Information.Purpose);
            }
        }

        private static void PrintBalance(string label, Balance balance)
        {
            if (balance == null)
                return;
            Console.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "  {0,-9} {1:yyyy-MM-dd} {2,-2} {3} {4,12:0.00} ({5})",
                label,
                balance.Date,
                balance.Mark.ToSwiftCode(),
                balance.Currency,
                balance.SignedAmount,
                balance.SourceTag));
        }

        private static string FindBundledSample()
        {
            var fromBase = Walk(AppDomain.CurrentDomain.BaseDirectory);
            if (fromBase != null)
                return fromBase;
            var fromCurrent = Walk(Directory.GetCurrentDirectory());
            if (fromCurrent != null)
                return fromCurrent;
            throw new FileNotFoundException("Could not find samples/statement.sta.");
        }

        private static string Walk(string start)
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "samples", "statement.sta");
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }

            return null;
        }
    }
}
