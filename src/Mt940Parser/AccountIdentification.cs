using System.Collections.Generic;
using System.Text;
using Mt940Parser.Parsing;

namespace Mt940Parser
{
    /// <summary>
    /// Account from tag :25:. Accepts <c>bank/account</c>, an IBAN, or <c>BIC/IBAN</c>,
    /// and the four DBS Hong Kong forms: account number, <c>BIC/account</c>,
    /// <c>BIC/account/name</c>, and <c>account/name</c>.
    /// </summary>
    public sealed class AccountIdentification
    {
        private readonly string _raw;
        private readonly string _bankCode;
        private readonly string _accountNumber;
        private readonly string _accountName;
        private readonly string _iban;
        private readonly string _bic;

        /// <summary>
        /// Parses an account identification. The raw value is kept; bank code, IBAN, and account name are filled when recognized.
        /// </summary>
        public AccountIdentification(string raw)
        {
            _raw = raw == null ? string.Empty : raw.Trim();
            if (_raw.Length == 0)
                return;

            var parts = SplitSlashes(_raw);
            if (parts.Count >= 3)
            {
                // DBS option iii: BIC / account number / account name.
                _bankCode = EmptyToNull(parts[0]);
                _accountNumber = EmptyToNull(parts[1]);
                _accountName = EmptyToNull(JoinTail(parts, 2));
            }
            else if (parts.Count == 2 && IsDbsAccountAndName(parts[0], parts[1]))
            {
                // DBS option iv: account number / account name (guide example :25:388888888/ABC COMPANY).
                _accountNumber = EmptyToNull(parts[0]);
                _accountName = EmptyToNull(parts[1]);
            }
            else if (parts.Count == 2)
            {
                _bankCode = EmptyToNull(parts[0]);
                _accountNumber = EmptyToNull(parts[1]);
            }
            else
            {
                _accountNumber = EmptyToNull(parts[0]);
            }

            var accountCompact = AccountTokens.Compact(_accountNumber);
            if (AccountTokens.IsIban(accountCompact))
                _iban = accountCompact;

            var bankCompact = AccountTokens.Compact(_bankCode);
            if (AccountTokens.IsBic(bankCompact))
                _bic = bankCompact;
        }

        /// <summary>Original :25: value, trimmed.</summary>
        public string Raw
        {
            get { return _raw; }
        }

        /// <summary>Text before the first slash, such as a BLZ or BIC. Null when there is no slash.</summary>
        public string BankCode
        {
            get { return _bankCode; }
        }

        /// <summary>Text after the slash, or the whole value when there is no slash.</summary>
        public string AccountNumber
        {
            get { return _accountNumber; }
        }

        /// <summary>
        /// Account name from DBS Field 25 when the value is <c>BIC/account/name</c> or <c>account/name</c>.
        /// Null for <c>bank/account</c> and for an account number alone.
        /// </summary>
        public string AccountName
        {
            get { return _accountName; }
        }

        /// <summary>Account number when it is an IBAN, otherwise null. Spaces are removed.</summary>
        public string Iban
        {
            get { return _iban; }
        }

        /// <summary>Bank code when it is a BIC, otherwise null.</summary>
        public string Bic
        {
            get { return _bic; }
        }

        /// <summary>Returns <see cref="Raw"/>.</summary>
        public override string ToString()
        {
            return Raw;
        }

        private static string EmptyToNull(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static List<string> SplitSlashes(string raw)
        {
            var parts = new List<string>();
            var start = 0;
            for (int i = 0; i <= raw.Length; i++)
            {
                if (i == raw.Length || raw[i] == '/')
                {
                    parts.Add(raw.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }

            return parts;
        }

        private static string JoinTail(IList<string> parts, int start)
        {
            var builder = new StringBuilder();
            for (int i = start; i < parts.Count; i++)
            {
                if (builder.Length > 0)
                    builder.Append('/');
                builder.Append(parts[i]);
            }

            return builder.ToString();
        }

        private static bool IsDbsAccountAndName(string left, string right)
        {
            // A numeric account and a name that contains a letter. Classic bank/account
            // (BLZ/number) keeps a numeric account on the right, so it is left unchanged.
            if (!IsAllDigits(left))
                return false;
            if (AccountTokens.IsIban(AccountTokens.Compact(right)))
                return false;
            return ContainsLetter(right);
        }

        private static bool IsAllDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsDigit(value[i]))
                    return false;
            }

            return true;
        }

        private static bool ContainsLetter(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetter(value[i]))
                    return true;
            }

            return false;
        }
    }
}
