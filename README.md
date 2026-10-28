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
| `:20:` | `Mt940Statement.TransactionReference` |
| `:21:` | `RelatedReference` |
| `:25:` | `Account` (`AccountIdentification`: bank/account, IBAN, or BIC/IBAN) |
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

`:61:` also reads an optional funds code, the four-character transaction type (`NTRF`, `NCHK`, …), the customer reference (the literal `NONREF` is kept), the bank reference after `//`, and supplementary details on the following lines.

## Tag :86:

Both common layouts are read from the same narrative:

- German fields: an optional 3-digit code, then `?00` posting text, `?10` primanota, `?20`–`?29` purpose, `?30` bank code, `?31` account, `?32`–`?33` name. Line wraps are joined before the codes are split, so a BIC broken across a 65-character line is reconstructed.
- Structured codes: `/EREF/`, `/KREF/`, `/MREF/`, `/CRED/`, `/SVWZ/`, `/IBAN/`, `/BIC/`, `/NAME/`, and any other `/CODE/` of 2–4 letters. The `CODE+value` form (`EREF+`, `SVWZ+`, …) is read as well. When both forms carry the same code, the slash form wins.
- DBS Hong Kong Appendix 2 prefixes are registered in `DbsField86Prefixes` and stored on `CodeFields`: `TD`, `BENM`, `BB`, `BA`, `REMI`, `EXCH`, `RA`, `ORDP`, `OB`, `OTH1`, `OTH2`, `OTH3`, `DF`, `BC`, `CREF`, `CRFX`, `OA`, `PREF`, `PURP`, `PV`, `VA`, `VN`. Line wraps are joined before the prefixes are split. `CustomerReference` (on the transaction and on `Mt940Information`) is the `/CREF/` value. When `/CREF/` is absent, `/KREF/` is used instead.

`Purpose` prefers `SVWZ`, then German `?20`–`?29` concatenated, then `REMI`, then the whole text when it has no codes. `CounterpartyIban` and `CounterpartyBic` are filled from `/IBAN/` and `/BIC/` or from `?31` and `?30` when those fields match.

## Layout

```
Mt940Parser.sln
src/Mt940Parser/             class library (net48)
src/Mt940Parser.Sample/      console sample and self-tests (net48)
samples/statement.sta        two statements covering the tags above
```
