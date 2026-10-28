using System;
using System.Collections.Generic;

namespace Mt940Parser.Parsing
{
    internal static class BalanceParser
    {
        public static bool TryParse(
            string tag,
            string content,
            int lineNumber,
            IList<Mt940ParseException> errors,
            out Balance balance)
        {
            balance = null;
            var sourceTag = ":" + tag + ":";
            var text = SingleLine(content);
            if (text.Length == 0)
            {
                errors.Add(new Mt940ParseException("Balance is empty.", lineNumber, sourceTag));
                return false;
            }

            DebitCreditMark mark;
            int markLength;
            if (!DebitCreditMarks.TryParse(text, 0, out mark, out markLength))
            {
                errors.Add(new Mt940ParseException("Balance debit/credit mark is missing.", lineNumber, sourceTag));
                return false;
            }

            if (text.Length < markLength + 6 + 3 + 1)
            {
                errors.Add(new Mt940ParseException("Balance is incomplete.", lineNumber, sourceTag));
                return false;
            }

            DateTime date;
            if (!SwiftDate.TryParseYyMmDd(text.Substring(markLength, 6), out date))
            {
                errors.Add(new Mt940ParseException("Balance date is not a valid YYMMDD.", lineNumber, sourceTag));
                return false;
            }

            var currency = text.Substring(markLength + 6, 3);
            if (!IsCurrency(currency))
            {
                errors.Add(new Mt940ParseException("Balance currency must be three letters.", lineNumber, sourceTag));
                return false;
            }

            decimal amount;
            var amountText = text.Substring(markLength + 9).Trim();
            if (!SwiftAmount.TryParse(amountText, out amount))
            {
                errors.Add(new Mt940ParseException("Balance amount is invalid.", lineNumber, sourceTag));
                return false;
            }

            balance = new Balance(MapKind(tag), mark, date, currency, amount, text, sourceTag);
            return true;
        }

        private static BalanceKind MapKind(string tag)
        {
            switch (tag)
            {
                case "60F":
                    return BalanceKind.Opening;
                case "60M":
                    return BalanceKind.IntermediateOpening;
                case "62F":
                    return BalanceKind.Closing;
                case "62M":
                    return BalanceKind.IntermediateClosing;
                case "64":
                    return BalanceKind.ClosingAvailable;
                case "65":
                    return BalanceKind.ForwardAvailable;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tag));
            }
        }

        private static bool IsCurrency(string currency)
        {
            if (currency == null || currency.Length != 3)
                return false;
            for (int i = 0; i < 3; i++)
            {
                if (!char.IsLetter(currency[i]))
                    return false;
            }

            return true;
        }

        private static string SingleLine(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;
            return content.Replace("\n", string.Empty).Trim();
        }
    }
}
