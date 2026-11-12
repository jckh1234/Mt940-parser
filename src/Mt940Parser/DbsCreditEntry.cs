using System;
using System.Collections.Generic;

namespace Mt940Parser
{
    /// <summary>
    /// One Field 61 credit line (debit/credit indicator C) reduced to the values the daily job stores.
    /// </summary>
    public sealed class DbsCreditEntry
    {
        /// <summary>
        /// Creates a credit row. <paramref name="amount"/> is the Field 61 amount (zero or positive).
        /// </summary>
        public DbsCreditEntry(
            string field20AccountNumber,
            DateTime valueDate,
            decimal amount,
            string supplementaryTimestamp,
            string transactionDescription,
            string fpsProxy,
            string payerName,
            string customerReference,
            string transParticular,
            string paymentPurposeCode,
            string remittanceBankBicCode)
        {
            if (string.IsNullOrWhiteSpace(field20AccountNumber))
                throw new ArgumentException("Field 20 account number is required.", nameof(field20AccountNumber));
            if (amount < 0m)
                throw new ArgumentOutOfRangeException(nameof(amount));

            Field20AccountNumber = field20AccountNumber.Trim();
            ValueDate = valueDate.Date;
            Amount = amount;
            SupplementaryTimestamp = EmptyToNull(supplementaryTimestamp);
            TransactionDescription = EmptyToNull(transactionDescription);
            FpsProxy = EmptyToNull(fpsProxy);
            PayerName = EmptyToNull(payerName);
            CustomerReference = EmptyToNull(customerReference);
            TransParticular = EmptyToNull(transParticular);
            PaymentPurposeCode = EmptyToNull(paymentPurposeCode);
            RemittanceBankBicCode = EmptyToNull(remittanceBankBicCode);
        }

        /// <summary>Account number from :20:.</summary>
        public string Field20AccountNumber { get; }

        /// <summary>Field 61 value date.</summary>
        public DateTime ValueDate { get; }

        /// <summary>Field 61 amount. Absolute, and positive for a credit.</summary>
        public decimal Amount { get; }

        /// <summary>Field 61 supplementary timestamp (HHMMSS). Null when the line has none.</summary>
        public string SupplementaryTimestamp { get; }

        /// <summary>Field 86 transaction description (?TD?). Null when absent.</summary>
        public string TransactionDescription { get; }

        /// <summary>Field 86 FPS proxy (?PV?). Null when absent.</summary>
        public string FpsProxy { get; }

        /// <summary>Field 86 payer name (?ORDP?). Null when absent.</summary>
        public string PayerName { get; }

        /// <summary>Field 86 customer reference (?CREF?). Null when absent. ?KREF? is not used.</summary>
        public string CustomerReference { get; }

        /// <summary>Field 86 transaction particular (?OTH1?). Null when absent.</summary>
        public string TransParticular { get; }

        /// <summary>Field 86 payment purpose code (?PURP?). Null when absent.</summary>
        public string PaymentPurposeCode { get; }

        /// <summary>Field 86 remittance bank BIC code (?BC?). Null when absent.</summary>
        public string RemittanceBankBicCode { get; }

        /// <summary>
        /// Credit lines whose statement :20: equals <paramref name="field20AccountNumber"/>.
        /// Debits, reversals, and expected credits are counted in <paramref name="skippedNonCreditLines"/>
        /// and are not returned. Statements for other accounts are counted in
        /// <paramref name="skippedOtherStatements"/>.
        /// </summary>
        public static IReadOnlyList<DbsCreditEntry> Select(
            Mt940Document document,
            string field20AccountNumber,
            out int skippedOtherStatements,
            out int skippedNonCreditLines)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (field20AccountNumber == null)
                throw new ArgumentNullException(nameof(field20AccountNumber));

            var account = field20AccountNumber.Trim();
            if (account.Length == 0)
                throw new ArgumentException("Field 20 account number is required.", nameof(field20AccountNumber));

            skippedOtherStatements = 0;
            skippedNonCreditLines = 0;
            var entries = new List<DbsCreditEntry>();
            for (int s = 0; s < document.Statements.Count; s++)
            {
                var statement = document.Statements[s];
                var reference = statement.TransactionReference == null
                    ? null
                    : statement.TransactionReference.Trim();
                if (!string.Equals(reference, account, StringComparison.Ordinal))
                {
                    skippedOtherStatements++;
                    continue;
                }

                for (int t = 0; t < statement.Transactions.Count; t++)
                {
                    var transaction = statement.Transactions[t];
                    if (!transaction.IsCredit)
                    {
                        skippedNonCreditLines++;
                        continue;
                    }

                    entries.Add(FromTransaction(account, transaction));
                }
            }

            return entries.AsReadOnly();
        }

        private static DbsCreditEntry FromTransaction(string account, Mt940Transaction transaction)
        {
            var information = transaction.Information;
            return new DbsCreditEntry(
                account,
                transaction.ValueDate,
                transaction.Amount,
                transaction.SupplementaryTimestamp,
                information == null ? null : information.TransactionDescription,
                information == null ? null : information.FpsProxy,
                information == null ? null : information.PayerName,
                information == null ? null : information.GetCodeField(DbsField86Prefixes.CustomerReferencePrefix),
                information == null ? null : information.TransParticular,
                information == null ? null : information.PaymentPurposeCode,
                information == null ? null : information.RemittanceBankBicCode);
        }

        private static string EmptyToNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return value.Trim();
        }
    }
}
