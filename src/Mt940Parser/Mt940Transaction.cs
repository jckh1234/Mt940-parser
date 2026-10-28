using System;
using System.Globalization;

namespace Mt940Parser
{
    /// <summary>
    /// One statement line (:61:) and its optional :86: narrative.
    /// <see cref="Amount"/> is absolute. <see cref="SignedAmount"/> is negative for debits, RC, and ED.
    /// </summary>
    public sealed class Mt940Transaction
    {
        /// <summary>
        /// Creates a transaction. <paramref name="amount"/> must be zero or positive.
        /// </summary>
        public Mt940Transaction(
            DateTime valueDate,
            DateTime? entryDate,
            DebitCreditMark mark,
            string fundsCode,
            decimal amount,
            string transactionType,
            string reference,
            string bankReference,
            string supplementaryDetails,
            Mt940Information information,
            string raw)
        {
            if (amount < 0m)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (string.IsNullOrEmpty(transactionType))
                throw new ArgumentException("Transaction type is required.", nameof(transactionType));

            ValueDate = valueDate.Date;
            EntryDate = entryDate.HasValue ? entryDate.Value.Date : (DateTime?)null;
            Mark = mark;
            FundsCode = string.IsNullOrEmpty(fundsCode) ? null : fundsCode;
            Amount = amount;
            TransactionType = transactionType;
            Reference = string.IsNullOrEmpty(reference) ? null : reference;
            BankReference = string.IsNullOrEmpty(bankReference) ? null : bankReference;
            SupplementaryDetails = string.IsNullOrEmpty(supplementaryDetails) ? null : supplementaryDetails;
            Information = information;
            Raw = raw ?? string.Empty;
        }

        /// <summary>Value date (YYMMDD).</summary>
        public DateTime ValueDate { get; }

        /// <summary>Entry date (MMDD, year taken from the value date). Null when the field is omitted.</summary>
        public DateTime? EntryDate { get; }

        /// <summary>Debit/credit mark.</summary>
        public DebitCreditMark Mark { get; }

        /// <summary>Optional funds code (third character of the currency). Null when omitted.</summary>
        public string FundsCode { get; }

        /// <summary>Absolute amount. Zero or positive.</summary>
        public decimal Amount { get; }

        /// <summary>Amount with sign. Negative for debits, RC, and ED.</summary>
        public decimal SignedAmount
        {
            get { return SignedAmounts.Apply(Amount, Mark); }
        }

        /// <summary>Four-character transaction type, such as NTRF or NCHK.</summary>
        public string TransactionType { get; }

        /// <summary>Reference for the account owner. Null when absent. The literal NONREF is preserved.</summary>
        public string Reference { get; }

        /// <summary>Account-servicing institution reference (the // field). Null when absent.</summary>
        public string BankReference { get; }

        /// <summary>Supplementary details from continuation lines of :61:. Null when absent.</summary>
        public string SupplementaryDetails { get; }

        /// <summary>:86: narrative attached to this line. Null when the line has no :86:.</summary>
        public Mt940Information Information { get; }

        /// <summary>
        /// Customer reference from <see cref="Information"/>. DBS <c>/CREF/</c> wins over SEPA <c>/KREF/</c>.
        /// Null when the line has no narrative or neither code.
        /// </summary>
        public string CustomerReference
        {
            get { return Information == null ? null : Information.CustomerReference; }
        }

        /// <summary>Original :61: body, including supplementary lines.</summary>
        public string Raw { get; }

        /// <summary>Returns the value date, mark, and signed amount.</summary>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd} {1} {2}",
                ValueDate,
                Mark.ToSwiftCode(),
                SignedAmount);
        }

        internal Mt940Transaction WithInformation(Mt940Information information)
        {
            return new Mt940Transaction(
                ValueDate,
                EntryDate,
                Mark,
                FundsCode,
                Amount,
                TransactionType,
                Reference,
                BankReference,
                SupplementaryDetails,
                information,
                Raw);
        }
    }
}
