using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mt940Parser
{
    /// <summary>
    /// DBS Hong Kong Limited Appendix 2 prefixes for structured Field 86 (<c>:86:</c>).
    /// Keys are the <c>/PREFIX/</c> codes. Values are the field names from that table.
    /// </summary>
    public static class DbsField86Prefixes
    {
        /// <summary>Prefix whose value is the customer reference (<c>/CREF/</c>).</summary>
        public const string CustomerReferencePrefix = "CREF";

        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "TD", "Transaction Description" },
            { "BENM", "Beneficiary Name" },
            { "BB", "Beneficiary Bank Name" },
            { "BA", "Beneficiary Account Number" },
            { "REMI", "Payment Details" },
            { "EXCH", "SETTLE_FX_EXCH_RATE" },
            { "RA", "Remittance Currency and Amount" },
            { "ORDP", "Payer Name" },
            { "OB", "Payer Bank Name" },
            { "OTH1", "TRANS PARTICULAR" },
            { "OTH2", "TRANS REMARKS 1" },
            { "OTH3", "TRANS PARTICULAR 2" },
            { "DF", "DDA Reference" },
            { "BC", "Remittance Bank Bic Code" },
            { CustomerReferencePrefix, "Customer Reference" },
            { "CRFX", "Settlement FX Reference" },
            { "OA", "Supplementary Information 1" },
            { "PREF", "Bank reference" },
            { "PURP", "Payment Purpose Code" },
            { "PV", "FPS Proxy" },
            { "VA", "Virtual Account Number" },
            { "VN", "Virtual Account Name" }
        };

        /// <summary>
        /// DBS Field 86 prefix codes mapped to their field names.
        /// </summary>
        public static IReadOnlyDictionary<string, string> FieldNames { get; } = new ReadOnlyDictionary<string, string>(Names);

        /// <summary>
        /// Returns the Appendix 2 field name for <paramref name="prefix"/>, or null when it is not a DBS prefix.
        /// Lookup is case-insensitive.
        /// </summary>
        public static string GetFieldName(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return null;

            string name;
            if (Names.TryGetValue(prefix.ToUpperInvariant(), out name))
                return name;
            return null;
        }
    }
}
