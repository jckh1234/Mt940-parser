using System;
using System.Globalization;

namespace Mt940Parser
{
    /// <summary>
    /// One balance from :60F:, :60M:, :62F:, :62M:, :64:, or :65:.
    /// <see cref="Amount"/> is absolute. <see cref="SignedAmount"/> is negative for debits, RC, and ED.
    /// </summary>
    public sealed class Balance
    {
        /// <summary>
        /// Creates a balance. <paramref name="amount"/> must be zero or positive.
        /// </summary>
        public Balance(
            BalanceKind kind,
            DebitCreditMark mark,
            DateTime date,
            string currency,
            decimal amount,
            string raw,
            string sourceTag)
        {
            if (amount < 0m)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency is required.", nameof(currency));

            Kind = kind;
            Mark = mark;
            Date = date.Date;
            Currency = currency.Trim().ToUpperInvariant();
            Amount = amount;
            Raw = raw ?? string.Empty;
            SourceTag = sourceTag ?? string.Empty;
        }

        /// <summary>Which balance tag this value came from.</summary>
        public BalanceKind Kind { get; }

        /// <summary>Debit/credit mark.</summary>
        public DebitCreditMark Mark { get; }

        /// <summary>Balance date (the availability date for :65:).</summary>
        public DateTime Date { get; }

        /// <summary>ISO currency code.</summary>
        public string Currency { get; }

        /// <summary>Absolute amount. Zero or positive.</summary>
        public decimal Amount { get; }

        /// <summary>Amount with sign. Negative for debits, RC, and ED.</summary>
        public decimal SignedAmount
        {
            get { return SignedAmounts.Apply(Amount, Mark); }
        }

        /// <summary>True for :60M: and :62M:.</summary>
        public bool IsIntermediate
        {
            get
            {
                return Kind == BalanceKind.IntermediateOpening
                    || Kind == BalanceKind.IntermediateClosing;
            }
        }

        /// <summary>Original tag value.</summary>
        public string Raw { get; }

        /// <summary>Source tag, such as ":60F:".</summary>
        public string SourceTag { get; }

        /// <summary>Returns the mark, date, currency, and signed amount.</summary>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1:yyyy-MM-dd} {2} {3}",
                Mark.ToSwiftCode(),
                Date,
                Currency,
                SignedAmount);
        }
    }
}
