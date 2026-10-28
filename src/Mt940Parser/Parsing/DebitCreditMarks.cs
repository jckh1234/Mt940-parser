using System;

namespace Mt940Parser.Parsing
{
    internal static class DebitCreditMarks
    {
        public static bool TryParse(string text, int index, out DebitCreditMark mark, out int length)
        {
            mark = DebitCreditMark.Credit;
            length = 0;
            if (text == null || index < 0 || index >= text.Length)
                return false;

            var first = char.ToUpperInvariant(text[index]);
            var second = index + 1 < text.Length ? char.ToUpperInvariant(text[index + 1]) : '\0';
            if (first == 'R' && second == 'C')
            {
                mark = DebitCreditMark.ReversalCredit;
                length = 2;
                return true;
            }

            if (first == 'R' && second == 'D')
            {
                mark = DebitCreditMark.ReversalDebit;
                length = 2;
                return true;
            }

            if (first == 'E' && second == 'C')
            {
                mark = DebitCreditMark.ExpectedCredit;
                length = 2;
                return true;
            }

            if (first == 'E' && second == 'D')
            {
                mark = DebitCreditMark.ExpectedDebit;
                length = 2;
                return true;
            }

            if (first == 'C')
            {
                mark = DebitCreditMark.Credit;
                length = 1;
                return true;
            }

            if (first == 'D')
            {
                mark = DebitCreditMark.Debit;
                length = 1;
                return true;
            }

            return false;
        }
    }
}
