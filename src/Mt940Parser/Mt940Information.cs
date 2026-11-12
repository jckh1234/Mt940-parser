using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Mt940Parser.Parsing;

namespace Mt940Parser
{
    /// <summary>
    /// Structured :86: narrative. German <c>?00</c> fields, structured <c>?CODE?</c> (and <c>CODE+</c>) fields,
    /// and DBS Hong Kong Appendix 2 prefixes are all exposed.
    /// Convenience properties prefer SEPA purpose and name when those codes are present.
    /// </summary>
    public sealed class Mt940Information
    {
        private readonly string _raw;
        private readonly string _transactionCode;
        private readonly IReadOnlyDictionary<string, string> _germanFields;
        private readonly IReadOnlyDictionary<string, string> _codeFields;
        private readonly string _bookingText;
        private readonly string _primanota;
        private readonly string _purpose;
        private readonly string _sepaPurpose;
        private readonly string _remittanceInformation;
        private readonly string _transactionDescription;
        private readonly string _beneficiaryName;
        private readonly string _fpsProxy;
        private readonly string _payerName;
        private readonly string _transParticular;
        private readonly string _paymentPurposeCode;
        private readonly string _remittanceBankBicCode;
        private readonly string _counterpartyName;
        private readonly string _counterpartyBankCode;
        private readonly string _counterpartyAccount;
        private readonly string _counterpartyIban;
        private readonly string _counterpartyBic;
        private readonly string _endToEndReference;
        private readonly string _customerReference;
        private readonly string _mandateReference;
        private readonly string _creditorIdentifier;

        /// <summary>
        /// Creates narrative details. Dictionaries are copied.
        /// </summary>
        public Mt940Information(
            string raw,
            string transactionCode,
            IDictionary<string, string> germanFields,
            IDictionary<string, string> codeFields)
        {
            _raw = raw ?? string.Empty;
            _transactionCode = string.IsNullOrEmpty(transactionCode) ? null : transactionCode;
            _germanFields = Copy(germanFields);
            _codeFields = Copy(codeFields);

            _bookingText = GetGerman("00");
            _primanota = GetGerman("10");
            _counterpartyBankCode = GetGerman("30");
            _counterpartyAccount = GetGerman("31");

            var germanName = ConcatGerman(32, 33);
            _counterpartyName = germanName ?? FirstCode("NAME", "BENM", "ORDP");

            _endToEndReference = GetCode("EREF");
            _customerReference = GetCode(DbsField86Prefixes.CustomerReferencePrefix) ?? GetCode("KREF");
            _mandateReference = GetCode("MREF");
            _creditorIdentifier = GetCode("CRED");
            _sepaPurpose = GetCode("SVWZ");
            _remittanceInformation = GetCode("REMI");
            _transactionDescription = GetCode("TD");
            _beneficiaryName = GetCode("BENM");
            _fpsProxy = GetCode("PV");
            _payerName = GetCode("ORDP");
            _transParticular = GetCode("OTH1");
            _paymentPurposeCode = GetCode("PURP");
            _remittanceBankBicCode = GetCode("BC");

            var ibanFromCode = AccountTokens.NormalizeIban(GetCode("IBAN"));
            var ibanFromAccount = AccountTokens.NormalizeIban(_counterpartyAccount);
            if (ibanFromCode != null)
                _counterpartyIban = ibanFromCode;
            else if (ibanFromAccount != null)
                _counterpartyIban = ibanFromAccount;

            var bicFromCode = AccountTokens.NormalizeBic(GetCode("BIC"));
            var bicFromBank = AccountTokens.NormalizeBic(_counterpartyBankCode);
            if (bicFromCode != null)
                _counterpartyBic = bicFromCode;
            else if (bicFromBank != null)
                _counterpartyBic = bicFromBank;

            var germanPurpose = ConcatGerman(20, 29);
            if (!string.IsNullOrEmpty(_sepaPurpose))
                _purpose = _sepaPurpose;
            else if (!string.IsNullOrEmpty(germanPurpose))
                _purpose = germanPurpose;
            else if (!string.IsNullOrEmpty(_remittanceInformation))
                _purpose = _remittanceInformation;
            else if (_germanFields.Count == 0 && _codeFields.Count == 0)
            {
                var trimmed = _raw.Trim();
                _purpose = trimmed.Length == 0 ? null : CollapseNewlines(trimmed);
            }
        }

        /// <summary>Original :86: body. Line wraps are preserved.</summary>
        public string Raw
        {
            get { return _raw; }
        }

        /// <summary>Leading three-digit German business-transaction code (GVC), or null.</summary>
        public string TransactionCode
        {
            get { return _transactionCode; }
        }

        /// <summary>German <c>?XX</c> fields. Keys are two digits ("00", "20"). Repeated codes are concatenated.</summary>
        public IReadOnlyDictionary<string, string> GermanFields
        {
            get { return _germanFields; }
        }

        /// <summary>
        /// <c>?CODE?</c> and <c>CODE+</c> fields, including every DBS Appendix 2 prefix
        /// (<see cref="DbsField86Prefixes"/>). Keys are uppercase codes such as "CREF", "OTH1", "EREF", and "SVWZ".
        /// The <c>?</c> form wins when the same code appears in both forms.
        /// </summary>
        public IReadOnlyDictionary<string, string> CodeFields
        {
            get { return _codeFields; }
        }

        /// <summary>Posting text from ?00.</summary>
        public string BookingText
        {
            get { return _bookingText; }
        }

        /// <summary>Primanota from ?10.</summary>
        public string Primanota
        {
            get { return _primanota; }
        }

        /// <summary>
        /// Best purpose text: ?SVWZ? or SVWZ+, otherwise ?20 through ?29 concatenated,
        /// otherwise ?REMI?, otherwise the whole narrative when it has no codes.
        /// </summary>
        public string Purpose
        {
            get { return _purpose; }
        }

        /// <summary>SEPA purpose from SVWZ, or null.</summary>
        public string SepaPurpose
        {
            get { return _sepaPurpose; }
        }

        /// <summary>Remittance information from REMI, or null.</summary>
        public string RemittanceInformation
        {
            get { return _remittanceInformation; }
        }

        /// <summary>DBS transaction description from ?TD?, or null.</summary>
        public string TransactionDescription
        {
            get { return _transactionDescription; }
        }

        /// <summary>DBS beneficiary name from ?BENM?, or null.</summary>
        public string BeneficiaryName
        {
            get { return _beneficiaryName; }
        }

        /// <summary>DBS FPS proxy from ?PV?, or null.</summary>
        public string FpsProxy
        {
            get { return _fpsProxy; }
        }

        /// <summary>DBS payer name from ?ORDP?, or null.</summary>
        public string PayerName
        {
            get { return _payerName; }
        }

        /// <summary>DBS transaction particular from ?OTH1?, or null.</summary>
        public string TransParticular
        {
            get { return _transParticular; }
        }

        /// <summary>DBS payment purpose code from ?PURP?, or null.</summary>
        public string PaymentPurposeCode
        {
            get { return _paymentPurposeCode; }
        }

        /// <summary>
        /// DBS remittance bank BIC code from ?BC?. The guide's credit example uses a short code such as 004.
        /// Stored as written. Null when the prefix is absent.
        /// </summary>
        public string RemittanceBankBicCode
        {
            get { return _remittanceBankBicCode; }
        }

        /// <summary>Counterparty name from ?32 and ?33, otherwise NAME, BENM, or ORDP.</summary>
        public string CounterpartyName
        {
            get { return _counterpartyName; }
        }

        /// <summary>Raw ?30 bank code (BLZ or BIC).</summary>
        public string CounterpartyBankCode
        {
            get { return _counterpartyBankCode; }
        }

        /// <summary>Raw ?31 account number or IBAN.</summary>
        public string CounterpartyAccount
        {
            get { return _counterpartyAccount; }
        }

        /// <summary>Counterparty IBAN from ?IBAN? or from ?31 when that field is an IBAN.</summary>
        public string CounterpartyIban
        {
            get { return _counterpartyIban; }
        }

        /// <summary>Counterparty BIC from ?BIC? or from ?30 when that field is a BIC.</summary>
        public string CounterpartyBic
        {
            get { return _counterpartyBic; }
        }

        /// <summary>End-to-end reference (EREF).</summary>
        public string EndToEndReference
        {
            get { return _endToEndReference; }
        }

        /// <summary>
        /// Customer reference. DBS <c>?CREF?</c> when that prefix is present, otherwise <c>?KREF?</c>.
        /// </summary>
        public string CustomerReference
        {
            get { return _customerReference; }
        }

        /// <summary>Mandate reference (MREF).</summary>
        public string MandateReference
        {
            get { return _mandateReference; }
        }

        /// <summary>Creditor scheme identifier (CRED).</summary>
        public string CreditorIdentifier
        {
            get { return _creditorIdentifier; }
        }

        /// <summary>Returns the ?XX field, or null.</summary>
        public string GetGermanField(string code)
        {
            if (code == null)
                throw new ArgumentNullException("code");
            return GetGerman(code);
        }

        /// <summary>Returns the ?CODE? field, or null. Lookup is case-insensitive.</summary>
        public string GetCodeField(string code)
        {
            if (code == null)
                throw new ArgumentNullException("code");
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
