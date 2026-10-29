using System;
using System.Collections.Generic;

namespace Mt940Parser
{
    /// <summary>
    /// One row from DBS Hong Kong Appendix 1 (SWIFT transaction code and DBS transaction code).
    /// </summary>
    public sealed class DbsTransactionCode
    {
        /// <summary>
        /// Creates one appendix row. <paramref name="creditDebit"/> is <c>CR</c> or <c>DR</c>.
        /// </summary>
        public DbsTransactionCode(string description, string creditDebit, string swiftCode, string code)
        {
            if (string.IsNullOrEmpty(description))
                throw new ArgumentException("Description is required.", nameof(description));
            if (string.IsNullOrEmpty(creditDebit))
                throw new ArgumentException("Credit/debit mark is required.", nameof(creditDebit));
            if (string.IsNullOrEmpty(swiftCode))
                throw new ArgumentException("SWIFT code is required.", nameof(swiftCode));
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("DBS code is required.", nameof(code));

            Description = description;
            CreditDebit = creditDebit;
            SwiftCode = swiftCode;
            Code = code;
        }

        /// <summary>Transaction description in tag 86 (max 15 characters in the guide).</summary>
        public string Description { get; }

        /// <summary><c>CR</c> or <c>DR</c>.</summary>
        public string CreditDebit { get; }

        /// <summary>SWIFT transaction code used as tag 61 subfield 6, without the leading N.</summary>
        public string SwiftCode { get; }

        /// <summary>DBS transaction code used as tag 61 supplementary subfield 9.</summary>
        public string Code { get; }
    }

    /// <summary>
    /// Appendix 1 lookup. Some DBS codes are listed more than once.
    /// </summary>
    public static class DbsTransactionCodes
    {
        private static readonly DbsTransactionCode[] Rows = new DbsTransactionCode[]
        {
            Row("OUTGOING ACT", "DR", "TRF", "506"),
            Row("OUTGOING TT", "DR", "TRF", "508"),
            Row("OUTGOING CHATS", "DR", "TRF", "495"),
            Row("PAYROLL", "DR", "TRF", "673"),
            Row("AUTOPAY", "DR", "TRF", "462"),
            Row("DIR DEBIT", "DR", "DDT", "452"),
            Row("DD CO ISSUED", "DR", "TRF", "484"),
            Row("CHEQUE WITTHDR", "DR", "CHK", "475"),
            Row("BANK CHEQUE", "DR", "TRF", "482"),
            Row("TD PLACEMENT", "DR", "CMT", "601"),
            Row("SWEEP TO", "DR", "CMS", "656"),
            Row("REMITTANCE FEE", "DR", "CHG", "698"),
            Row("MSC FEES", "DR", "CHG", "698"),
            Row("REVERSAL", "DR", "RTI", "552"),
            Row("CASH WITHDRAW", "DR", "MSC", "487"),
            Row("MISCELLANEOUS", "DR", "MSC", "699"),
            Row("XBACH", "DR", "TRF", "510"),
            Row("XBACH BULK", "DR", "TRF", "507"),
            Row("CHEQUE RETURN", "DR", "RTI", "566"),
            Row("FPS", "DR", "TRF", "501"),
            Row("FPS DDT", "DR", "DDT", "500"),
            Row("FPS RFD", "DR", "RTI", "555"),
            Row("FPS SALARY RFD", "DR", "RTI", "555"),
            Row("OVERDRAFT INT", "DR", "ODC", "564"),
            Row("REV DIR DEBIT", "DR", "RTI", "558"),
            Row("INCOMING ACT", "CR", "TRF", "206"),
            Row("INCOMING TT", "CR", "TRF", "208"),
            Row("INCOMING CHATS", "CR", "TRF", "195"),
            Row("DIR DEBIT", "CR", "DDT", "165"),
            Row("ACH CREDIT", "CR", "TRF", "142"),
            Row("PAYROLL CR", "CR", "TRF", "373"),
            Row("CHEQUE DEPOSIT", "CR", "CHK", "175"),
            Row("TD CLOSURE", "CR", "CMT", "301"),
            Row("SWEEP FROM", "CR", "CMS", "347"),
            Row("CASH DEPOSIT", "CR", "MSC", "174"),
            Row("CREDIT INT", "CR", "INT", "354"),
            Row("BILL PAYMENT", "CR", "MSC", "399"),
            Row("MISCELLANEOUS", "CR", "MSC", "399"),
            Row("AUTOPAY RETURN", "CR", "RTI", "168"),
            Row("AUTOPAY RFD", "CR", "RTI", "168"),
            Row("FPS", "CR", "TRF", "201"),
            Row("FPS DDT", "CR", "DDT", "169"),
            Row("FPS PAYROLL RFD", "CR", "RTI", "257"),
            Row("FPS RFD", "CR", "RTI", "257"),
            Row("FPS RTI", "CR", "RTI", "257"),
            Row("PAYROLL RFD", "CR", "RTI", "168"),
            Row("REFUND", "CR", "RTI", "398"),
            Row("REVERSAL", "CR", "RTI", "252")
        };

        /// <summary>Every Appendix 1 row, in guide order.</summary>
        public static IReadOnlyList<DbsTransactionCode> All
        {
            get { return Array.AsReadOnly(Rows); }
        }

        /// <summary>
        /// Rows whose DBS code equals <paramref name="code"/>. Empty when the code is unknown.
        /// </summary>
        public static IReadOnlyList<DbsTransactionCode> Find(string code)
        {
            var matches = new List<DbsTransactionCode>();
            if (string.IsNullOrEmpty(code))
                return matches.AsReadOnly();

            var key = code.Trim();
            for (int i = 0; i < Rows.Length; i++)
            {
                if (string.Equals(Rows[i].Code, key, StringComparison.Ordinal))
                    matches.Add(Rows[i]);
            }

            return matches.AsReadOnly();
        }

        /// <summary>
        /// Appendix description for <paramref name="code"/>. Multiple rows are joined with " / ".
        /// Null when the code is unknown.
        /// </summary>
        public static string Describe(string code)
        {
            var matches = Find(code);
            if (matches.Count == 0)
                return null;
            if (matches.Count == 1)
                return matches[0].Description;

            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < matches.Count; i++)
            {
                if (i > 0)
                    builder.Append(" / ");
                builder.Append(matches[i].Description);
            }

            return builder.ToString();
        }

        private static DbsTransactionCode Row(string description, string creditDebit, string swiftCode, string code)
        {
            return new DbsTransactionCode(description, creditDebit, swiftCode, code);
        }
    }
}
