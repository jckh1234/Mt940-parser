using System;

namespace Mt940Parser
{
    /// <summary>
    /// SWIFT debit/credit mark from a balance or statement line.
    /// </summary>
    public enum DebitCreditMark
    {
        /// <summary>Credit (C). Increases the balance.</summary>
        Credit = 0,

        /// <summary>Debit (D). Decreases the balance.</summary>
        Debit = 1,

        /// <summary>Reversal of credit (RC). Decreases the balance.</summary>
        ReversalCredit = 2,

        /// <summary>Reversal of debit (RD). Increases the balance.</summary>
        ReversalDebit = 3,

        /// <summary>Expected credit (EC). Increases the balance.</summary>
        ExpectedCredit = 4,

        /// <summary>Expected debit (ED). Decreases the balance.</summary>
        ExpectedDebit = 5
    }

    /// <summary>
    /// Helpers for <see cref="DebitCreditMark"/>.
    /// </summary>
    public static class DebitCreditMarkExtensions
    {
        /// <summary>
        /// Returns the SWIFT code: C, D, RC, RD, EC, or ED.
        /// </summary>
        public static string ToSwiftCode(this DebitCreditMark mark)
        {
            switch (mark)
            {
                case DebitCreditMark.Credit:
                    return "C";
                case DebitCreditMark.Debit:
                    return "D";
                case DebitCreditMark.ReversalCredit:
                    return "RC";
                case DebitCreditMark.ReversalDebit:
                    return "RD";
                case DebitCreditMark.ExpectedCredit:
                    return "EC";
                case DebitCreditMark.ExpectedDebit:
                    return "ED";
                default:
                    throw new ArgumentOutOfRangeException(nameof(mark));
            }
        }

        /// <summary>
        /// True when the mark reduces the balance: debit, reversal of credit (RC), or expected debit (ED).
        /// </summary>
        public static bool IsNegative(this DebitCreditMark mark)
        {
            return mark == DebitCreditMark.Debit
                || mark == DebitCreditMark.ReversalCredit
                || mark == DebitCreditMark.ExpectedDebit;
        }
    }
}
