using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Mt940Parser.Sample
{
    internal static class SelfTest
    {
        public static void Run(Mt940Document document, string text, string path)
        {
            AssertSample(document);
            AssertRoundTrip(text, path);
            AssertInlineCases();
            AssertDbsField86();
            DbsSelfTest.Run();
        }

        private static void AssertSample(Mt940Document document)
        {
            Equal("statement count", 2, document.Statements.Count);

            var first = document.Statements[0];
            Equal("ref 1", "STARTUMS", first.TransactionReference);
            Null("related 1", first.RelatedReference);
            Equal("account raw", "50010517/1234567890", first.Account.Raw);
            Equal("bank code", "50010517", first.Account.BankCode);
            Equal("account number", "1234567890", first.Account.AccountNumber);
            Null("owner iban", first.Account.Iban);
            Equal("number", "00001", first.StatementNumber.Number);
            Equal("sequence", "001", first.StatementNumber.Sequence);
            Equal("number value", 1, first.StatementNumber.NumberValue);
            Equal("sequence value", 1, first.StatementNumber.SequenceValue);
            Equal("number tag", ":28C:", first.StatementNumber.SourceTag);
            True("28C", first.StatementNumber.IsContinuationTag);
            Equal("currency", "EUR", first.Currency);

            Equal("opening kind", BalanceKind.Opening, first.OpeningBalance.Kind);
            Equal("opening tag", ":60F:", first.OpeningBalance.SourceTag);
            False("opening intermediate", first.OpeningBalance.IsIntermediate);
            Equal("opening date", new DateTime(2023, 10, 1), first.OpeningBalance.Date);
            Equal("opening mark", DebitCreditMark.Credit, first.OpeningBalance.Mark);
            Equal("opening amount", 1000.00m, first.OpeningBalance.Amount);
            Equal("opening signed", 1000.00m, first.OpeningBalance.SignedAmount);

            Equal("tx count 1", 4, first.Transactions.Count);
            var credit = first.Transactions[0];
            Equal("tx1 date", new DateTime(2023, 10, 2), credit.ValueDate);
            Equal("tx1 entry", new DateTime(2023, 10, 2), credit.EntryDate);
            Equal("tx1 mark", DebitCreditMark.Credit, credit.Mark);
            Equal("tx1 code", "C", credit.Mark.ToSwiftCode());
            False("tx1 negative", credit.Mark.IsNegative());
            Null("tx1 funds", credit.FundsCode);
            Equal("tx1 amount", 250.00m, credit.Amount);
            Equal("tx1 signed", 250.00m, credit.SignedAmount);
            Equal("tx1 type", "NTRF", credit.TransactionType);
            Equal("tx1 ref", "NONREF", credit.Reference);
            Equal("tx1 bank", "BANKREF01", credit.BankReference);
            Null("tx1 supp", credit.SupplementaryDetails);
            Equal("tx1 gvc", "166", credit.Information.TransactionCode);
            Equal("tx1 booking", "GUTSCHRIFT", credit.Information.BookingText);
            Equal("tx1 primanota", "9249", credit.Information.Primanota);
            Equal("tx1 purpose", "RECHNUNG 4711 VOM 01.10.2023", credit.Information.Purpose);
            Equal("tx1 name", "MAX MUSTERMANN", credit.Information.CounterpartyName);
            Equal("tx1 bic", "COBADEFFXXX", credit.Information.CounterpartyBic);
            Equal("tx1 iban", "DE89370400440532013000", credit.Information.CounterpartyIban);
            Equal("tx1 german 20", "RECHNUNG 4711 VOM 01.10.2023", credit.Information.GetGermanField("20"));
            Null("tx1 eref", credit.Information.EndToEndReference);

            var debit = first.Transactions[1];
            Equal("tx2 mark", DebitCreditMark.Debit, debit.Mark);
            True("tx2 negative", debit.Mark.IsNegative());
            Equal("tx2 signed", -75.50m, debit.SignedAmount);
            Equal("tx2 ref", "REF987654321", debit.Reference);
            Null("tx2 bank", debit.BankReference);
            Equal("tx2 date", new DateTime(2023, 10, 3), debit.ValueDate);
            Equal("tx2 entry", new DateTime(2023, 10, 3), debit.EntryDate);
            Equal("tx2 gvc", "177", debit.Information.TransactionCode);
            Equal("tx2 booking", "UEBERWEISUNG", debit.Information.BookingText);
            Equal("tx2 purpose", "MIETE OKTOBER WHG 4 OG LINKS", debit.Information.Purpose);
            Equal("tx2 bic", "GENODEF1S01", debit.Information.CounterpartyBic);
            Equal("tx2 bank field", "GENODEF1S01", debit.Information.CounterpartyBankCode);
            Equal("tx2 iban", "DE12500105170648489890", debit.Information.CounterpartyIban);
            Equal("tx2 name", "VERMIETER GMBH", debit.Information.CounterpartyName);
            True("tx2 raw joined", debit.Information.Raw.Replace("\n", string.Empty).Contains("GENODEF1S01"));

            var reversal = first.Transactions[2];
            Equal("tx3 mark", DebitCreditMark.ReversalCredit, reversal.Mark);
            Equal("tx3 code", "RC", reversal.Mark.ToSwiftCode());
            Equal("tx3 signed", -10.00m, reversal.SignedAmount);
            Equal("tx3 amount", 10.00m, reversal.Amount);
            Equal("tx3 date", new DateTime(2023, 10, 4), reversal.ValueDate);
            Null("tx3 entry", reversal.EntryDate);
            Equal("tx3 type", "NRTI", reversal.TransactionType);
            Equal("tx3 ref", "REVERSAL01", reversal.Reference);
            Equal("tx3 purpose", "STORNO GUTSCHRIFT", reversal.Information.Purpose);
            Equal("tx3 booking", "RUECKGABE", reversal.Information.BookingText);

            var funds = first.Transactions[3];
            Equal("tx4 date", new DateTime(2023, 10, 15), funds.ValueDate);
            Equal("tx4 entry", new DateTime(2023, 10, 15), funds.EntryDate);
            Equal("tx4 funds", "R", funds.FundsCode);
            Equal("tx4 signed", 500.00m, funds.SignedAmount);
            Equal("tx4 ref", "REFWITHFUNDS", funds.Reference);
            Equal("tx4 bank", "B1", funds.BankReference);
            Equal("tx4 supp", "FEE WAIVED", funds.SupplementaryDetails);
            Null("tx4 dbs code", funds.DbsTransactionCode);
            Null("tx4 dbs time", funds.SupplementaryTimestamp);
            True("tx4 is credit", funds.IsCredit);
            Equal("tx4 gvc", "805", funds.Information.TransactionCode);
            Equal("tx4 booking", "ENTGELT", funds.Information.BookingText);
            Equal("tx4 german 20", "?EREF?FEE-15?SVWZ?Kontofuehrung Oktober", funds.Information.GetGermanField("20"));
            Equal("tx4 eref", "FEE-15", funds.Information.EndToEndReference);
            Equal("tx4 svwz", "Kontofuehrung Oktober", funds.Information.SepaPurpose);
            Equal("tx4 purpose", "Kontofuehrung Oktober", funds.Information.Purpose);

            Equal("signed total 1", 664.50m, first.SignedTransactionTotal);
            Equal("closing signed", 1664.50m, first.ClosingBalance.SignedAmount);
            Equal(
                "balance identity 1",
                first.OpeningBalance.SignedAmount + first.SignedTransactionTotal,
                first.ClosingBalance.SignedAmount);
            Equal("closing kind", BalanceKind.Closing, first.ClosingBalance.Kind);
            Equal("closing tag", ":62F:", first.ClosingBalance.SourceTag);
            Equal("closing date", new DateTime(2023, 10, 15), first.ClosingBalance.Date);
            False("closing intermediate", first.ClosingBalance.IsIntermediate);
            Equal("available signed", 1664.50m, first.ClosingAvailableBalance.SignedAmount);
            Equal("available kind", BalanceKind.ClosingAvailable, first.ClosingAvailableBalance.Kind);
            Equal("forward count", 2, first.ForwardAvailableBalances.Count);
            Equal("fwd1 date", new DateTime(2023, 10, 16), first.ForwardAvailableBalances[0].Date);
            Equal("fwd1 amount", 1600.00m, first.ForwardAvailableBalances[0].SignedAmount);
            Equal("fwd1 kind", BalanceKind.ForwardAvailable, first.ForwardAvailableBalances[0].Kind);
            Equal("fwd2 date", new DateTime(2023, 10, 17), first.ForwardAvailableBalances[1].Date);
            Equal("fwd2 amount", 1664.50m, first.ForwardAvailableBalances[1].SignedAmount);
            Equal("info", "Hinweis: disponierter Betrag", first.Information.Purpose);

            var second = document.Statements[1];
            Equal("ref 2", "STMT0002", second.TransactionReference);
            Equal("related 2", "RELREF0002", second.RelatedReference);
            Equal("iban", "DE44500105175407324931", second.Account.Iban);
            Equal("account is iban", second.Account.Iban, second.Account.AccountNumber);
            Null("no bank", second.Account.BankCode);
            Equal("num 2", "5", second.StatementNumber.Number);
            Equal("seq 2", "2", second.StatementNumber.Sequence);
            Equal("tag 2", ":28:", second.StatementNumber.SourceTag);
            False("not 28C", second.StatementNumber.IsContinuationTag);
            True("opening M", second.OpeningBalance.IsIntermediate);
            Equal("opening M kind", BalanceKind.IntermediateOpening, second.OpeningBalance.Kind);
            Equal("opening M tag", ":60M:", second.OpeningBalance.SourceTag);
            Equal("opening M date", new DateTime(2023, 11, 1), second.OpeningBalance.Date);
            Equal("opening M signed", -50.00m, second.OpeningBalance.SignedAmount);
            Equal("tx count 2", 1, second.Transactions.Count);

            var expected = second.Transactions[0];
            Equal("ed mark", DebitCreditMark.ExpectedDebit, expected.Mark);
            Equal("ed code", "ED", expected.Mark.ToSwiftCode());
            True("ed negative", expected.Mark.IsNegative());
            Equal("ed signed", -15.25m, expected.SignedAmount);
            Equal("ed date", new DateTime(2023, 11, 2), expected.ValueDate);
            Equal("ed entry", new DateTime(2023, 11, 2), expected.EntryDate);
            Equal("ed type", "NCHK", expected.TransactionType);
            Equal("ed ref", "CHQ0001", expected.Reference);
            Equal("ed bank", "998877", expected.BankReference);
            Equal("ed supp", "EXTRA DETAIL", expected.SupplementaryDetails);
            Null("ed gvc", expected.Information.TransactionCode);
            Equal("eref", "INV-2023-99", expected.Information.EndToEndReference);
            Equal("kref", "CUST-44", expected.Information.CustomerReference);
            Equal("mref", "MANDATE1", expected.Information.MandateReference);
            Equal("cred", "DE98ZZZ09999999999", expected.Information.CreditorIdentifier);
            Equal("svwz", "Rechnung November", expected.Information.SepaPurpose);
            Equal("purpose 2", "Rechnung November", expected.Information.Purpose);
            Equal("cp iban", "DE12500105175407324931", expected.Information.CounterpartyIban);
            Equal("cp bic", "INGDDEFFXXX", expected.Information.CounterpartyBic);
            Equal("cp name", "ACME GMBH", expected.Information.CounterpartyName);
            Equal("code eref", "INV-2023-99", expected.Information.GetCodeField("eref"));

            True("closing M", second.ClosingBalance.IsIntermediate);
            Equal("closing M kind", BalanceKind.IntermediateClosing, second.ClosingBalance.Kind);
            Equal("closing M signed", -65.25m, second.ClosingBalance.SignedAmount);
            Equal(
                "balance identity 2",
                second.OpeningBalance.SignedAmount + second.SignedTransactionTotal,
                second.ClosingBalance.SignedAmount);
            Equal("avail 2", -40.00m, second.ClosingAvailableBalance.SignedAmount);
            Equal("avail 2 mark", DebitCreditMark.Debit, second.ClosingAvailableBalance.Mark);
            Equal("forward 2", 0, second.ForwardAvailableBalances.Count);
            Null("no statement info", second.Information);
        }

        private static void AssertRoundTrip(string text, string path)
        {
            Mt940Document parsed;
            IList<Mt940ParseException> errors;
            True("tryparse sample", Mt940Document.TryParse(text, out parsed, out errors));
            Equal("tryparse errors", 0, errors.Count);
            Equal("tryparse count", 2, parsed.Statements.Count);

            var crlf = text.Replace("\n", "\r\n");
            var fromCrlf = Mt940Document.Parse(crlf);
            Equal("crlf count", 2, fromCrlf.Statements.Count);
            Equal("crlf ref", "STARTUMS", fromCrlf.Statements[0].TransactionReference);

            using (var stream = new MemoryStream(File.ReadAllBytes(path)))
            {
                var fromStream = Mt940Document.Parse(stream);
                True("stream still open", stream.CanRead);
                Equal("stream ref", "STMT0002", fromStream.Statements[1].TransactionReference);
                Equal("stream total", 664.50m, fromStream.Statements[0].SignedTransactionTotal);
            }

            var bom = "\uFEFF" + text;
            Equal("bom ref", "STARTUMS", Mt940Document.Parse(bom).Statements[0].TransactionReference);
        }

        private static void AssertInlineCases()
        {
            var signs = Mt940Document.Parse(@"
:20:SIGNS
:25:ACC
:60F:C240101EUR10,00
:61:240101RD2,50NTRFREF1
:61:240102EC3,00NTRFREF2
:61:240103D1,00NTRFREF3
:61:240104C4,25NTRFREF4
:62F:C240104EUR18,75
");
            var txs = signs.Statements[0].Transactions;
            Equal("rd", DebitCreditMark.ReversalDebit, txs[0].Mark);
            Equal("rd signed", 2.50m, txs[0].SignedAmount);
            False("rd negative", txs[0].Mark.IsNegative());
            Equal("ec", DebitCreditMark.ExpectedCredit, txs[1].Mark);
            Equal("ec signed", 3.00m, txs[1].SignedAmount);
            Equal("d signed", -1.00m, txs[2].SignedAmount);
            Equal("c signed", 4.25m, txs[3].SignedAmount);
            Equal("signs close", 18.75m, signs.Statements[0].ClosingBalance.SignedAmount);
            Equal(
                "signs identity",
                10.00m + 2.50m + 3.00m - 1.00m + 4.25m,
                signs.Statements[0].ClosingBalance.SignedAmount);

            var dates = Mt940Document.Parse(@"
:20:DATES
:25:ACC
:60F:C990101EUR1,00
:61:2312310102C1,00NTRFX//B
:61:2401021231D1,00NTRFY
:62F:C260228EUR1,00
:64:C790101EUR1,00
:65:C240229EUR1,00
");
            Equal("year 99", new DateTime(1999, 1, 1), dates.Statements[0].OpeningBalance.Date);
            Equal("roll forward", new DateTime(2024, 1, 2), dates.Statements[0].Transactions[0].EntryDate);
            Equal("value dec", new DateTime(2023, 12, 31), dates.Statements[0].Transactions[0].ValueDate);
            Equal("roll back", new DateTime(2023, 12, 31), dates.Statements[0].Transactions[1].EntryDate);
            Equal("year 26", new DateTime(2026, 2, 28), dates.Statements[0].ClosingBalance.Date);
            Equal("year 79", new DateTime(2079, 1, 1), dates.Statements[0].ClosingAvailableBalance.Date);
            Equal("leap", new DateTime(2024, 2, 29), dates.Statements[0].ForwardAvailableBalances[0].Date);

            var amounts = Mt940Document.Parse(@"
:20:AMT
:25:ACC
:60F:C240101EUR1.234,56
:61:240101C1000,NTRFREF
:62F:C240101EUR2234,56
");
            Equal("thousands", 1234.56m, amounts.Statements[0].OpeningBalance.Amount);
            Equal("trailing comma", 1000m, amounts.Statements[0].Transactions[0].Amount);
            IList<Mt940ParseException> amountErrors;
            Mt940Document amountDoc;
            False(
                "currency mismatch",
                Mt940Document.TryParse(@"
:20:AMT
:25:ACC
:60F:C240101EUR1.234,56
:61:240101C1000,NTRFREF
:62F:D240101USD1,234.56
", out amountDoc, out amountErrors));
            True("mismatch kept opening", amountDoc.Statements[0].OpeningBalance.Amount == 1234.56m);
            True("mismatch has error", amountErrors.Count >= 1);
            Equal("mismatch tag", ":62F:", amountErrors[0].Tag);

            var narrative = Mt940Document.Parse(@":20:N
:25:A
:86:Statement note
:60F:C240101EUR0,00
:61:240101C1,00NTRFREF
:86:166?00ONE
:86:?20HELLO?21WORLD
:61:240102C1,00NTRFREF2
:86:166?00SEPA-GUTSCHRIFT?20EREF+INV9?21SVWZ+Honorar Oktober?31DE89 3704 0044 0532 0130 00?32HANS MUELLER
:62F:C240102EUR2,00
");
            Equal("stmt note", "Statement note", narrative.Statements[0].Information.Purpose);
            Equal("joined booking", "ONE", narrative.Statements[0].Transactions[0].Information.BookingText);
            Equal("joined purpose", "HELLOWORLD", narrative.Statements[0].Transactions[0].Information.Purpose);
            var sepa = narrative.Statements[0].Transactions[1].Information;
            Equal("plus eref", "INV9", sepa.EndToEndReference);
            Equal("plus svwz", "Honorar Oktober", sepa.SepaPurpose);
            Equal("plus purpose", "Honorar Oktober", sepa.Purpose);
            Equal("plus iban", "DE89370400440532013000", sepa.CounterpartyIban);
            Equal("plus name", "HANS MUELLER", sepa.CounterpartyName);

            var split = Mt940Document.Parse(@":20:FIRST
:25:A/1
:60F:C240101EUR1,00
:62F:C240101EUR1,00
:20:SECOND
:25:DEUTDEFFXXX/DE89370400440532013000
:28:42
:60M:D240102EUR2,00
:62M:D240102EUR2,00
");
            Equal("order 1", "FIRST", split.Statements[0].TransactionReference);
            Equal("order 2", "SECOND", split.Statements[1].TransactionReference);
            Equal("bare number", "42", split.Statements[1].StatementNumber.Number);
            Null("bare sequence", split.Statements[1].StatementNumber.Sequence);
            Equal("bic slash", "DEUTDEFFXXX", split.Statements[1].Account.Bic);
            Equal("iban slash", "DE89370400440532013000", split.Statements[1].Account.Iban);

            var padded = Mt940Document.Parse("  :20:PAD\r\n  :25:DE89 3704 0044 0532 0130 00\r\n");
            Equal("padded ref", "PAD", padded.Statements[0].TransactionReference);
            Equal("spaced iban", "DE89370400440532013000", padded.Statements[0].Account.Iban);

            var ignored = Mt940Document.Parse(@":20:U
:25:A
:13D:2401011200+0000
:90D:1EUR1,00
:60F:c240101eur1,50
:62F:C240101EUR1,50
");
            Equal("lower currency", "EUR", ignored.Statements[0].OpeningBalance.Currency);
            Equal("lower mark", DebitCreditMark.Credit, ignored.Statements[0].OpeningBalance.Mark);
            Equal("lower amount", 1.50m, ignored.Statements[0].OpeningBalance.SignedAmount);

            Mt940Document badDoc;
            IList<Mt940ParseException> badErrors;
            False("bad try", Mt940Document.TryParse(":20:BAD\n:25:1\n:61:NOTVALID\n:62F:C240101EUR0,00\n", out badDoc, out badErrors));
            Equal("bad line", 3, badErrors[0].LineNumber);
            Equal("bad tag", ":61:", badErrors[0].Tag);
            True("bad message", badErrors[0].Message.Contains("Line 3") && badErrors[0].Message.Contains(":61:"));
            Equal("bad still has statement", "BAD", badDoc.Statements[0].TransactionReference);
            Equal("bad dropped tx", 0, badDoc.Statements[0].Transactions.Count);

            try
            {
                Mt940Document.Parse(":20:BAD\n:25:1\n:61:NOTVALID\n");
                throw new InvalidOperationException("Parse should have thrown.");
            }
            catch (Mt940ParseException ex)
            {
                Equal("throw line", 3, ex.LineNumber);
                Equal("throw tag", ":61:", ex.Tag);
            }

            False(
                "two errors",
                Mt940Document.TryParse(":20:BAD\n:60F:XXXX\n:61:NOTVALID\n", out badDoc, out badErrors));
            Equal("two count", 2, badErrors.Count);
            Equal("first bad tag", ":60F:", badErrors[0].Tag);
            Equal("first bad line", 2, badErrors[0].LineNumber);
            Equal("second bad tag", ":61:", badErrors[1].Tag);

            try
            {
                Mt940Document.Parse((string)null);
                throw new InvalidOperationException("Parse(null) should have thrown.");
            }
            catch (ArgumentNullException)
            {
            }

            False("null try", Mt940Document.TryParse((string)null, out badDoc, out badErrors));
            True("null error", badErrors[0].Message.Contains("null"));

            False("empty try", Mt940Document.TryParse("  \n\t", out badDoc, out badErrors));
            True("empty error", badErrors[0].Message.Contains("empty"));

            False("garbage", Mt940Document.TryParse("hello", out badDoc, out badErrors));
            True("garbage error", badErrors.Count > 0);

            False("bad number", Mt940Document.TryParse(":20:N\n:28C:ABC\n", out badDoc, out badErrors));
            Equal("bad number tag", ":28C:", badErrors[0].Tag);

            False("bad leap", Mt940Document.TryParse(":20:N\n:60F:C230229EUR1,00\n", out badDoc, out badErrors));
            Equal("bad leap tag", ":60F:", badErrors[0].Tag);

            var dup = ":20:D\n:60F:C240101EUR1,00\n:60M:C240101EUR2,00\n";
            False("dup opening", Mt940Document.TryParse(dup, out badDoc, out badErrors));
            Equal("kept first opening", 1.00m, badDoc.Statements[0].OpeningBalance.Amount);
            Equal("dup tag", ":60M:", badErrors[0].Tag);

            var zero = Mt940Document.Parse(":20:Z\n:61:240101D0,00NMSCNONREF\n");
            Equal("zero debit", 0m, zero.Statements[0].Transactions[0].SignedAmount);
            Equal("zero mark", DebitCreditMark.Debit, zero.Statements[0].Transactions[0].Mark);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(":20:U\n:61:240101C1,00NTRFREF\n:86:166?32M\u00dcLLER\n")))
            {
                var umlaut = Mt940Document.Parse(stream, Encoding.UTF8);
                Equal("umlaut", "M\u00dcLLER", umlaut.Statements[0].Transactions[0].Information.CounterpartyName);
                True("umlaut stream open", stream.CanRead);
            }

            var headerOnly = "{1:F01BANK}{4:\n";
            False("header only", Mt940Document.TryParse(headerOnly, out badDoc, out badErrors));
            True("header message", badErrors[0].Message.Contains("No MT940"));

            var minimal = Mt940Document.Parse(":20:ONLY\n");
            Equal("minimal", "ONLY", minimal.Statements[0].TransactionReference);
            Equal("minimal tx", 0, minimal.Statements[0].Transactions.Count);
            Null("minimal open", minimal.Statements[0].OpeningBalance);
        }

        private static void AssertDbsField86()
        {
            Equal("dbs prefix count", 22, DbsField86Prefixes.FieldNames.Count);
            Equal("dbs cref name", "Customer Reference", DbsField86Prefixes.GetFieldName("CREF"));
            Equal("dbs cref name lower", "Customer Reference", DbsField86Prefixes.GetFieldName("cref"));
            Equal("dbs oth1 name", "TRANS PARTICULAR", DbsField86Prefixes.FieldNames["OTH1"]);

            var statement = Mt940Document.Parse(@"
:20:DBS
:25:001234567
:61:260101C100,00NTRFNONREF
:86:?TD?INCOMING ACT?BENM?ACME LTD
?CREF?SOME_REF?OTH1?PARTICULAR?OTH2?REMARK ONE?OTH3?MORE
?REMI?INV/2026?RA?HKD100,00?ORDP?PAYER CO?BC?DBSSSGSG
").Statements[0];

            var tx = statement.Transactions[0];
            Equal("61 ref stays NONREF", "NONREF", tx.Reference);
            Equal("tx cref", "SOME_REF", tx.CustomerReference);
            Equal("info cref", "SOME_REF", tx.Information.CustomerReference);
            Equal("code cref", "SOME_REF", tx.Information.GetCodeField("CREF"));
            Equal("dbs td", "INCOMING ACT", tx.Information.GetCodeField("TD"));
            Equal("dbs benm", "ACME LTD", tx.Information.GetCodeField("BENM"));
            Equal("dbs oth1", "PARTICULAR", tx.Information.GetCodeField("OTH1"));
            Equal("dbs oth2", "REMARK ONE", tx.Information.GetCodeField("OTH2"));
            Equal("dbs oth3", "MORE", tx.Information.GetCodeField("OTH3"));
            Equal("dbs remi", "INV/2026", tx.Information.RemittanceInformation);
            Equal("dbs ra", "HKD100,00", tx.Information.GetCodeField("RA"));
            Equal("dbs ordp", "PAYER CO", tx.Information.GetCodeField("ORDP"));
            Equal("dbs bc", "DBSSSGSG", tx.Information.GetCodeField("BC"));
            Equal("name from benm", "ACME LTD", tx.Information.CounterpartyName);

            var both = Mt940Document.Parse(@"
:20:BOTH
:25:1
:61:260101C1,00NTRFREF
:86:KREF?SEPAREF?CREF?SOME_REF
").Statements[0].Transactions[0];
            Equal("cref wins", "SOME_REF", both.CustomerReference);
            Equal("kref kept", "SEPAREF", both.Information.GetCodeField("KREF"));

            var bare = Mt940Document.Parse(":20:X\n:61:260101C1,00NTRFNONREF\n").Statements[0].Transactions[0];
            Null("no narrative cref", bare.CustomerReference);

            var slash = Mt940Document.Parse(":20:SLASH\n:61:260101C1,00NTRFREF\n:86:/CREF/SOME_REF\n").Statements[0].Transactions[0];
            Null("slash cref ignored", slash.CustomerReference);
            Null("slash code ignored", slash.Information.GetCodeField("CREF"));
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
