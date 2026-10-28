using System.Collections.Generic;

namespace Mt940Parser
{
    /// <summary>
    /// One MT940 customer statement. A file may contain several, in source order.
    /// </summary>
    public sealed class Mt940Statement
    {
        /// <summary>
        /// Creates a statement. List arguments are copied.
        /// </summary>
        public Mt940Statement(
            string transactionReference,
            string relatedReference,
            AccountIdentification account,
            StatementNumber statementNumber,
            Balance openingBalance,
            Balance closingBalance,
            Balance closingAvailableBalance,
            IEnumerable<Balance> forwardAvailableBalances,
            IEnumerable<Mt940Transaction> transactions,
            Mt940Information information)
        {
            TransactionReference = string.IsNullOrEmpty(transactionReference) ? null : transactionReference;
            RelatedReference = string.IsNullOrEmpty(relatedReference) ? null : relatedReference;
            Account = account;
            StatementNumber = statementNumber;
            OpeningBalance = openingBalance;
            ClosingBalance = closingBalance;
            ClosingAvailableBalance = closingAvailableBalance;
            ForwardAvailableBalances = CopyBalances(forwardAvailableBalances);
            Transactions = CopyTransactions(transactions);
            Information = information;
        }

        /// <summary>Transaction reference from :20:.</summary>
        public string TransactionReference { get; }

        /// <summary>Related reference from :21:. Null when the tag is absent.</summary>
        public string RelatedReference { get; }

        /// <summary>Account from :25:. Null when the tag is absent.</summary>
        public AccountIdentification Account { get; }

        /// <summary>Statement number from :28: or :28C:. Null when the tag is absent.</summary>
        public StatementNumber StatementNumber { get; }

        /// <summary>Opening balance from :60F: or :60M:. Null when absent.</summary>
        public Balance OpeningBalance { get; }

        /// <summary>Closing balance from :62F: or :62M:. Null when absent.</summary>
        public Balance ClosingBalance { get; }

        /// <summary>Closing available balance from :64:. Null when absent.</summary>
        public Balance ClosingAvailableBalance { get; }

        /// <summary>Forward available balances from :65:, in file order.</summary>
        public IReadOnlyList<Balance> ForwardAvailableBalances { get; }

        /// <summary>Statement lines in file order.</summary>
        public IReadOnlyList<Mt940Transaction> Transactions { get; }

        /// <summary>
        /// Statement-level :86: text that was not attached to a transaction.
        /// Null when there is none.
        /// </summary>
        public Mt940Information Information { get; }

        /// <summary>
        /// Currency of the opening balance, otherwise the closing balance, otherwise :64:.
        /// Null when no balance carries a currency.
        /// </summary>
        public string Currency
        {
            get
            {
                if (OpeningBalance != null)
                    return OpeningBalance.Currency;
                if (ClosingBalance != null)
                    return ClosingBalance.Currency;
                if (ClosingAvailableBalance != null)
                    return ClosingAvailableBalance.Currency;
                return null;
            }
        }

        /// <summary>Sum of <see cref="Mt940Transaction.SignedAmount"/> for every transaction.</summary>
        public decimal SignedTransactionTotal
        {
            get
            {
                decimal total = 0m;
                for (int i = 0; i < Transactions.Count; i++)
                    total += Transactions[i].SignedAmount;
                return total;
            }
        }

        /// <summary>Returns the transaction reference.</summary>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(TransactionReference))
                return TransactionReference;
            return base.ToString();
        }

        private static IReadOnlyList<Balance> CopyBalances(IEnumerable<Balance> balances)
        {
            var copy = new List<Balance>();
            if (balances != null)
            {
                foreach (var balance in balances)
                {
                    if (balance != null)
                        copy.Add(balance);
                }
            }

            return copy.AsReadOnly();
        }

        private static IReadOnlyList<Mt940Transaction> CopyTransactions(IEnumerable<Mt940Transaction> transactions)
        {
            var copy = new List<Mt940Transaction>();
            if (transactions != null)
            {
                foreach (var transaction in transactions)
                {
                    if (transaction != null)
                        copy.Add(transaction);
                }
            }

            return copy.AsReadOnly();
        }
    }
}
