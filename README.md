# MT940 Parser

C# parser for SWIFT MT940 customer statements. The library targets **.NET Framework 4.8** and **C# 7.3**. It has no runtime dependencies. `Microsoft.NETFramework.ReferenceAssemblies` is a private build asset so the project can compile without a Framework targeting pack installed on the machine.

## Build

```bash
dotnet build Mt940Parser.sln -c Release
```

The sample is a .NET Framework executable. On Windows, with .NET Framework 4.8 installed:

```bash
src\Mt940Parser.Sample\bin\Release\net48\Mt940Parser.Sample.exe
```

On Linux, Mono can run the same executable:

```bash
mono src/Mt940Parser.Sample/bin/Release/net48/Mt940Parser.Sample.exe
mono src/Mt940Parser.Scheduler/bin/Release/net48/Mt940Parser.Scheduler.exe --help
```

With no arguments the sample parses `samples/statement.sta`, prints the statements, and runs self-tests. Pass a path to print another file; the bundled self-tests still run.

## Parse

```csharp
using System.IO;
using Mt940Parser;

Mt940Document document = Mt940Document.Parse(File.ReadAllText("statement.sta"));
// Mt940Document.Parse(stream) reads UTF-8 and leaves the stream open.
// Mt940Document.Parse(stream, encoding) uses another fallback encoding.

foreach (Mt940Statement statement in document.Statements)
{
    decimal opening = statement.OpeningBalance.SignedAmount;
    foreach (Mt940Transaction transaction in statement.Transactions)
    {
        decimal signed = transaction.SignedAmount;          // negative for D, RC, ED
        string purpose = transaction.Information?.Purpose;
        string name = transaction.Information?.CounterpartyName;
    }
}
```

`Parse` throws `Mt940ParseException` at the first problem. `LineNumber` is 1-based (0 when the failure is not on a line) and `Tag` is the SWIFT tag, for example `:61:`.

```csharp
Mt940Document document;
IList<Mt940ParseException> errors;
if (!Mt940Document.TryParse(text, out document, out errors))
{
    foreach (Mt940ParseException error in errors)
        Console.WriteLine("{0} (line {1}, tag {2})", error.Message, error.LineNumber, error.Tag);
}
```

`TryParse` still returns every statement it could read. Null content or a null stream is reported as an error and does not throw. `Parse(null)` throws `ArgumentNullException`.

## Statements

A file can hold several messages. They are returned in order. A new `:20:` starts the next statement, as does a SWIFT terminator (`-`, `-}`, or `$`). Envelope lines such as `{1:...}{2:...}{4:` are ignored. Unknown tags (`:13D:`, `:90C:`, `:90D:`, `:34F:`, and anything else outside the set below) are skipped.

| Tag | Model |
| --- | --- |
| `:20:` | `Mt940Statement.TransactionReference`. DBS Hong Kong stores the account number here (10–20 characters); `DbsAccountNumber` is the same value. |
| `:21:` | `RelatedReference` |
| `:25:` | `Account` (`AccountIdentification`: bank/account, IBAN, BIC/IBAN, or a DBS form — account, `BIC/account`, `BIC/account/name`, `account/name`) |
| `:28:` / `:28C:` | `StatementNumber` (leading zeros kept; `NumberValue` is numeric) |
| `:60F:` / `:60M:` | `OpeningBalance` (`BalanceKind.Opening` or `IntermediateOpening`) |
| `:61:` | `Transactions` |
| `:86:` | `Mt940Transaction.Information`, or statement `Information` when it does not follow a `:61:` |
| `:62F:` / `:62M:` | `ClosingBalance` |
| `:64:` | `ClosingAvailableBalance` |
| `:65:` | `ForwardAvailableBalances` (every occurrence, in order) |

## Amounts and dates

Amounts are `decimal`. The absolute value is `Amount`. `SignedAmount` is negative for **debit (D)**, **reversal of credit (RC)**, and **expected debit (ED)**. Credit (C), reversal of debit (RD), and expected credit (EC) stay positive. The same rule is `DebitCreditMarkExtensions.IsNegative`.

SWIFT amounts use a comma as the decimal separator (`1234,56` or `1234,`). A dot is accepted as a decimal separator, and `1.234,56` is treated as 1234.56.

Dates are `YYMMDD`. Years **00–79** are **2000–2079**. Years **80–99** are **1980–1999**. The `:61:` entry date is `MMDD`; the year comes from the value date and rolls across the new year when the two dates are more than six months apart.

`:61:` also reads an optional funds code (the third character of the currency, so HKD is `D`), the four-character transaction type (`NTRF`, `NCHK`, …), the customer reference (the literal `NONREF` is kept), the bank reference after `//`, and supplementary details on the following lines.

DBS supplementary details use `?` as the separator: a 3-digit Appendix 1 code, the posting time `HHMMSS`, and an optional term-deposit account (`201?100913`, `601?153045?TDACC9988`). Those are `DbsTransactionCode`, `SupplementaryTimestamp`, and `TermDepositAccount`. `DbsTransactionDescription` is the Appendix 1 name (`201` is `FPS`). A line that is not in that shape, such as `FEE WAIVED`, stays in `SupplementaryDetails` only. `IsCredit` is true only for indicator `C`.

## Tag :86:

Both common layouts are read from the same narrative:

- German fields: an optional 3-digit code, then `?00` posting text, `?10` primanota, `?20`–`?29` purpose, `?30` bank code, `?31` account, `?32`–`?33` name. Line wraps are joined before the codes are split, so a BIC broken across a 65-character line is reconstructed.
- Structured codes use `?` as the value separator: `?EREF?`, `?KREF?`, `?MREF?`, `?CRED?`, `?SVWZ?`, `?IBAN?`, `?BIC?`, `?NAME?`, and any other `?CODE?` of 2–4 letters (`?CODE?value`, or `CODE?value` when the code starts the narrative). The `CODE+value` form (`EREF+`, `SVWZ+`, …) is read as well. When both forms carry the same code, the `?` form wins.
- DBS Hong Kong Appendix 2 prefixes are registered in `DbsField86Prefixes` and stored on `CodeFields`: `TD`, `BENM`, `BB`, `BA`, `REMI`, `EXCH`, `RA`, `ORDP`, `OB`, `OTH1`, `OTH2`, `OTH3`, `DF`, `BC`, `CREF`, `CRFX`, `OA`, `PREF`, `PURP`, `PV`, `VA`, `VN`. The guide writes them as `TD?FPS?PV?10000006?ORDP?MISS CHAN?CREF?...` (`PREFIX?value`, or `?PREFIX?value` after the first field). Line wraps, including a break after every 65 characters, are joined before the prefixes are split, so a reference broken across the wrap is reconstructed. A numeric value such as `PV?10000006`, `OTH1?2609091009115XXXXX159`, or `BC?004` is not treated as a German `?dd` subfield. Named properties are `TransactionDescription` (TD), `FpsProxy` (PV), `PayerName` (ORDP), `TransParticular` (OTH1), `PaymentPurposeCode` (PURP), and `RemittanceBankBicCode` (BC). `CustomerReference` (on the transaction and on `Mt940Information`) is the `?CREF?` value. When `?CREF?` is absent, `?KREF?` is used instead. The daily credit row stores `?CREF?` only.

`Purpose` prefers `SVWZ`, then German `?20`–`?29` concatenated, then `REMI`, then the whole text when it has no codes. `CounterpartyIban` and `CounterpartyBic` are filled from `?IBAN?` and `?BIC?` or from `?31` and `?30` when those fields match.

## Daily credit load

`Mt940Parser.Scheduler` reads today's file from a shared folder and records one pseudo insert per credit line.

```bash
mono src/Mt940Parser.Scheduler/bin/Release/net48/Mt940Parser.Scheduler.exe \
  --folder /mnt/statements \
  --account 3888888888 \
  --connection "Server=db;Database=Mt940;" \
  --date 2026-10-29
```

Omit `--date` to use the machine's local date. The file name is `MT940.D` + `yyMMdd` + `.txt` (`MT940.D261029.txt` on 29 October 2026). Set the host clock to the statement date; DBS delivers the end-of-day file at about 04:00 Hong Kong time.

The job keeps statements whose Field 20 equals `--account`. Other accounts in the same file are ignored. Only Field 61 lines with debit/credit indicator `C` are loaded. For each of those lines it takes:

| Column | Source |
| --- | --- |
| Account number | Field 20 |
| Value date | Field 61 |
| Amount | Field 61 |
| Supplementary timestamp | Field 61 `code?HHMMSS` |
| Transaction description | Field 86 `TD` |
| FPS proxy | Field 86 `PV` |
| Payer name | Field 86 `ORDP` |
| Customer reference | Field 86 `CREF` |
| Transaction particular | Field 86 `OTH1` |
| Payment purpose code | Field 86 `PURP` |
| Remittance bank BIC code | Field 86 `BC` |

`CreditStatementStore.Insert` is the database step. It checks the connection string and returns the `INSERT` text. It does not open a connection. Missing Field 86 values are inserted as NULL and reported as warnings. A file that fails to parse is not loaded.

Exit codes: `0` success (including no credit lines), `1` invalid arguments, `2` folder or file not found, `3` parse error, `4` read error.

`samples/MT940.D260909.txt` is a two-account file in the DBS shape, including the credit example `201?100913` / `TD?FPS?...?BC?004`.

## Layout

```
Mt940Parser.sln
src/Mt940Parser/             class library (net48)
src/Mt940Parser.Sample/      console sample and self-tests (net48)
src/Mt940Parser.Scheduler/   daily shared-folder loader (net48)
samples/statement.sta        two statements covering the tags above
samples/MT940.D260909.txt    DBS multi-account credit sample
```
