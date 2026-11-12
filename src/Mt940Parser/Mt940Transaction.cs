using System;
using System.Globalization;
using Mt940Parser.Parsing;

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

            string dbsTransactionCode;
            string supplementaryTimestamp;
            string termDepositAccount;
            SupplementaryDetailsParser.Read(
                SupplementaryDetails,
                out dbsTransactionCode,
                out supplementaryTimestamp,
                out termDepositAccount);
            DbsTransactionCode = dbsTransactionCode;
            SupplementaryTimestamp = supplementaryTimestamp;
            TermDepositAccount = termDepositAccount;
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

        /// <summary>
        /// DBS transaction code from supplementary details (<c>201?100913</c> → <c>201</c>).
        /// Null when the supplementary line is not <c>code?HHMMSS</c>.
        /// </summary>
        public string DbsTransactionCode { get; }

        /// <summary>
        /// Posting timestamp (HHMMSS) from :61: supplementary details.
        /// For <c>201?100913</c> this is <c>100913</c>. Null when the line has no timestamp.
        /// </summary>
        public string SupplementaryTimestamp { get; }

        /// <summary>
        /// Term-deposit account number from supplementary details, when the bank sends a third
        /// <c>?</c> value after the timestamp. Null when absent.
        /// </summary>
        public string TermDepositAccount { get; }

        /// <summary>
        /// Appendix 1 description for <see cref="DbsTransactionCode"/>.
        /// When several rows share a code, descriptions are joined with " / ". Null when the code is unknown.
        /// </summary>
        public string DbsTransactionDescription
        {
            get { return DbsTransactionCodes.Describe(DbsTransactionCode); }
        }

        /// <summary>
        /// True when the debit/credit indicator is C. Reversals and expected credits are not included.
        /// </summary>
        public bool IsCredit
        {
            get { return Mark == DebitCreditMark.Credit; }
        }

        /// <summary>:86: narrative attached to this line. Null when the line has no :86:.</summary>
        public Mt940Information Information { get; }

        /// <summary>
        /// Customer reference from <see cref="Information"/>. DBS <c>?CREF?</c> wins over <c>?KREF?</c>.
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
