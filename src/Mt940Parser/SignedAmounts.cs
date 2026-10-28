using System;

namespace Mt940Parser
{
    /// <summary>
    /// Applies the MT940 sign rule: debits, RC, and ED are negative; credits, RD, and EC are positive.
    /// </summary>
    internal static class SignedAmounts
    {
        public static decimal Apply(decimal absoluteAmount, DebitCreditMark mark)
        {
            if (absoluteAmount < 0m)
                throw new ArgumentOutOfRangeException(nameof(absoluteAmount));

            if (mark.IsNegative())
                return decimal.Negate(absoluteAmount);

            return absoluteAmount;
        }
    }
}
