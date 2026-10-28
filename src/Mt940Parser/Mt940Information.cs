using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Mt940Parser.Parsing;

namespace Mt940Parser
{
    /// <summary>
    /// Structured :86: narrative. German <c>?00</c> fields, SEPA <c>/CODE/</c> (and <c>CODE+</c>) fields,
    /// and DBS Hong Kong Appendix 2 prefixes are all exposed.
    /// Convenience properties prefer SEPA purpose and name when those codes are present.
    /// </summary>
    public sealed class Mt940Information
    {
        /// <summary>
        /// Creates narrative details. Dictionaries are copied.
        /// </summary>
        public Mt940Information(
            string raw,
            string transactionCode,
            IDictionary<string, string> germanFields,
            IDictionary<string, string> codeFields)
        {
            Raw = raw ?? string.Empty;
            TransactionCode = string.IsNullOrEmpty(transactionCode) ? null : transactionCode;
            GermanFields = Copy(germanFields);
            CodeFields = Copy(codeFields);

            BookingText = GetGerman("00");
            Primanota = GetGerman("10");
            CounterpartyBankCode = GetGerman("30");
            CounterpartyAccount = GetGerman("31");

            var germanName = ConcatGerman(32, 33);
            CounterpartyName = germanName ?? FirstCode("NAME", "BENM", "ORDP");

            EndToEndReference = GetCode("EREF");
            CustomerReference = GetCode(DbsField86Prefixes.CustomerReferencePrefix) ?? GetCode("KREF");
            MandateReference = GetCode("MREF");
            CreditorIdentifier = GetCode("CRED");
            SepaPurpose = GetCode("SVWZ");
            RemittanceInformation = GetCode("REMI");

            var ibanFromCode = AccountTokens.NormalizeIban(GetCode("IBAN"));
            var ibanFromAccount = AccountTokens.NormalizeIban(CounterpartyAccount);
            if (ibanFromCode != null)
                CounterpartyIban = ibanFromCode;
            else if (ibanFromAccount != null)
                CounterpartyIban = ibanFromAccount;

            var bicFromCode = AccountTokens.NormalizeBic(GetCode("BIC"));
            var bicFromBank = AccountTokens.NormalizeBic(CounterpartyBankCode);
            if (bicFromCode != null)
                CounterpartyBic = bicFromCode;
            else if (bicFromBank != null)
                CounterpartyBic = bicFromBank;

            var germanPurpose = ConcatGerman(20, 29);
            if (!string.IsNullOrEmpty(SepaPurpose))
                Purpose = SepaPurpose;
            else if (!string.IsNullOrEmpty(germanPurpose))
                Purpose = germanPurpose;
            else if (!string.IsNullOrEmpty(RemittanceInformation))
                Purpose = RemittanceInformation;
            else if (GermanFields.Count == 0 && CodeFields.Count == 0)
            {
                var trimmed = Raw.Trim();
                Purpose = trimmed.Length == 0 ? null : CollapseNewlines(trimmed);
            }
        }

        /// <summary>Original :86: body. Line wraps are preserved.</summary>
        public string Raw { get; }

        /// <summary>Leading three-digit German business-transaction code (GVC), or null.</summary>
        public string TransactionCode { get; }

        /// <summary>German <c>?XX</c> fields. Keys are two digits ("00", "20"). Repeated codes are concatenated.</summary>
        public IReadOnlyDictionary<string, string> GermanFields { get; }

        /// <summary>
        /// <c>/CODE/</c> and <c>CODE+</c> fields, including every DBS Appendix 2 prefix
        /// (<see cref="DbsField86Prefixes"/>). Keys are uppercase codes such as "CREF", "OTH1", "EREF", and "SVWZ".
        /// Slash form wins when the same code appears in both forms.
        /// </summary>
        public IReadOnlyDictionary<string, string> CodeFields { get; }

        /// <summary>Posting text from ?00.</summary>
        public string BookingText { get; }

        /// <summary>Primanota from ?10.</summary>
        public string Primanota { get; }

        /// <summary>
        /// Best purpose text: /SVWZ/ or SVWZ+, otherwise ?20 through ?29 concatenated,
        /// otherwise /REMI/, otherwise the whole narrative when it has no codes.
        /// </summary>
        public string Purpose { get; }

        /// <summary>SEPA purpose from SVWZ, or null.</summary>
        public string SepaPurpose { get; }

        /// <summary>Remittance information from REMI, or null.</summary>
        public string RemittanceInformation { get; }

        /// <summary>Counterparty name from ?32 and ?33, otherwise NAME, BENM, or ORDP.</summary>
        public string CounterpartyName { get; }

        /// <summary>Raw ?30 bank code (BLZ or BIC).</summary>
        public string CounterpartyBankCode { get; }

        /// <summary>Raw ?31 account number or IBAN.</summary>
        public string CounterpartyAccount { get; }

        /// <summary>Counterparty IBAN from /IBAN/ or from ?31 when that field is an IBAN.</summary>
        public string CounterpartyIban { get; }

        /// <summary>Counterparty BIC from /BIC/ or from ?30 when that field is a BIC.</summary>
        public string CounterpartyBic { get; }

        /// <summary>End-to-end reference (EREF).</summary>
        public string EndToEndReference { get; }

        /// <summary>
        /// Customer reference. DBS <c>/CREF/</c> when that prefix is present, otherwise SEPA <c>/KREF/</c>.
        /// </summary>
        public string CustomerReference { get; }

        /// <summary>Mandate reference (MREF).</summary>
        public string MandateReference { get; }

        /// <summary>Creditor scheme identifier (CRED).</summary>
        public string CreditorIdentifier { get; }

        /// <summary>Returns the ?XX field, or null.</summary>
        public string GetGermanField(string code)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));
            return GetGerman(code);
        }

        /// <summary>Returns the /CODE/ field, or null. Lookup is case-insensitive.</summary>
        public string GetCodeField(string code)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));
            return GetCode(code.ToUpperInvariant());
        }

        /// <summary>Returns the purpose, booking text, or raw narrative.</summary>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Purpose))
                return Purpose;
            if (!string.IsNullOrEmpty(BookingText))
                return BookingText;
            return Raw;
        }

        private string GetGerman(string code)
        {
            string value;
            if (GermanFields.TryGetValue(code, out value) && value.Length > 0)
                return value;
            return null;
        }

        private string GetCode(string code)
        {
            string value;
            if (CodeFields.TryGetValue(code, out value) && value.Length > 0)
                return value;
            return null;
        }

        private string FirstCode(params string[] codes)
        {
            for (int i = 0; i < codes.Length; i++)
            {
                var value = GetCode(codes[i]);
                if (value != null)
                    return value;
            }

            return null;
        }

        private string ConcatGerman(int from, int to)
        {
            var builder = new StringBuilder();
            var any = false;
            for (int i = from; i <= to; i++)
            {
                var key = i.ToString("00", CultureInfo.InvariantCulture);
                string value;
                if (GermanFields.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
                {
                    builder.Append(value);
                    any = true;
                }
            }

            return any ? builder.ToString() : null;
        }

        private static string CollapseNewlines(string text)
        {
            return text.Replace("\r", string.Empty).Replace("\n", string.Empty);
        }

        private static IReadOnlyDictionary<string, string> Copy(IDictionary<string, string> source)
        {
            var copy = new Dictionary<string, string>(StringComparer.Ordinal);
            if (source != null)
            {
                foreach (var pair in source)
                {
                    if (string.IsNullOrEmpty(pair.Key))
                        continue;
                    copy[pair.Key] = pair.Value ?? string.Empty;
                }
            }

            return new ReadOnlyDictionary<string, string>(copy);
        }
    }
}
