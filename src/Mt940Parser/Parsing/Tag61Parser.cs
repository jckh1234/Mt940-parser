using System;
using System.Collections.Generic;

namespace Mt940Parser.Parsing
{
    internal static class Tag61Parser
    {
        public static bool TryParse(
            string content,
            int lineNumber,
            IList<Mt940ParseException> errors,
            out Mt940Transaction transaction)
        {
            transaction = null;
            var text = content ?? string.Empty;
            var newline = text.IndexOf('\n');
            var first = (newline >= 0 ? text.Substring(0, newline) : text).Trim();
            string supplementary = null;
            if (newline >= 0)
            {
                supplementary = text.Substring(newline + 1).Trim();
                if (supplementary.Length == 0)
                    supplementary = null;
            }

            if (first.Length < 6 || !SwiftDate.IsAllDigits(first, 0, 6))
            {
                errors.Add(new Mt940ParseException("Transaction value date is missing or not YYMMDD.", lineNumber, ":61:"));
                return false;
            }

            DateTime valueDate;
            if (!SwiftDate.TryParseYyMmDd(first.Substring(0, 6), out valueDate))
            {
                errors.Add(new Mt940ParseException("Transaction value date is not a valid YYMMDD.", lineNumber, ":61:"));
                return false;
            }

            var index = 6;
            DateTime? entryDate = null;
            if (SwiftDate.IsAllDigits(first, index, 4) && MarkAt(first, index + 4))
            {
                DateTime entry;
                if (!SwiftDate.TryParseMmDd(first.Substring(index, 4), valueDate, out entry))
                {
                    errors.Add(new Mt940ParseException("Transaction entry date is not a valid MMDD.", lineNumber, ":61:"));
                    return false;
                }

                entryDate = entry;
                index += 4;
            }

            DebitCreditMark mark;
            int markLength;
            if (!DebitCreditMarks.TryParse(first, index, out mark, out markLength))
            {
                errors.Add(new Mt940ParseException("Transaction debit/credit mark is missing.", lineNumber, ":61:"));
                return false;
            }

            index += markLength;

            string fundsCode = null;
            if (index + 1 < first.Length && char.IsLetter(first[index]) && char.IsDigit(first[index + 1]))
            {
                fundsCode = first.Substring(index, 1).ToUpperInvariant();
                index++;
            }

            var amountStart = index;
            while (index < first.Length && IsAmountChar(first[index]))
                index++;

            if (index == amountStart)
            {
                errors.Add(new Mt940ParseException("Transaction amount is missing.", lineNumber, ":61:"));
                return false;
            }

            decimal amount;
            if (!SwiftAmount.TryParse(first.Substring(amountStart, index - amountStart), out amount))
            {
                errors.Add(new Mt940ParseException("Transaction amount is invalid.", lineNumber, ":61:"));
                return false;
            }

            if (index + 4 > first.Length || !IsTransactionType(first, index))
            {
                errors.Add(new Mt940ParseException("Transaction type must be a letter followed by three letters or digits.", lineNumber, ":61:"));
                return false;
            }

            var transactionType = first.Substring(index, 4).ToUpperInvariant();
            index += 4;
            var rest = first.Substring(index).Trim();
            string reference;
            string bankReference = null;
            var separator = rest.IndexOf("//", StringComparison.Ordinal);
            if (separator >= 0)
            {
                reference = rest.Substring(0, separator).Trim();
                bankReference = rest.Substring(separator + 2).Trim();
                if (bankReference.Length == 0)
                    bankReference = null;
            }
            else
            {
                reference = rest;
            }

            if (reference.Length == 0)
                reference = null;

            transaction = new Mt940Transaction(
                valueDate,
                entryDate,
                mark,
                fundsCode,
                amount,
                transactionType,
                reference,
                bankReference,
                supplementary,
                null,
                text.Trim());
            return true;
        }

        private static bool MarkAt(string text, int index)
        {
            DebitCreditMark mark;
            int length;
            return DebitCreditMarks.TryParse(text, index, out mark, out length);
        }

        private static bool IsAmountChar(char value)
        {
            return char.IsDigit(value) || value == ',' || value == '.';
        }

        private static bool IsTransactionType(string text, int index)
        {
            if (!char.IsLetter(text[index]))
                return false;
            for (int i = 1; i < 4; i++)
            {
                if (!char.IsLetterOrDigit(text[index + i]))
                    return false;
            }

            return true;
        }
    }
}
