using Mt940Parser.Parsing;

namespace Mt940Parser
{
    /// <summary>
    /// Account from tag :25:. Accepts <c>bank/account</c>, an IBAN, or <c>BIC/IBAN</c>.
    /// </summary>
    public sealed class AccountIdentification
    {
        /// <summary>
        /// Parses an account identification. The raw value is kept; bank code and IBAN are filled when recognized.
        /// </summary>
        public AccountIdentification(string raw)
        {
            Raw = raw == null ? string.Empty : raw.Trim();
            var slash = Raw.IndexOf('/');
            if (slash >= 0)
            {
                BankCode = EmptyToNull(Raw.Substring(0, slash).Trim());
                AccountNumber = EmptyToNull(Raw.Substring(slash + 1).Trim());
            }
            else if (Raw.Length > 0)
            {
                AccountNumber = Raw;
            }

            var accountCompact = AccountTokens.Compact(AccountNumber);
            if (AccountTokens.IsIban(accountCompact))
                Iban = accountCompact;

            var bankCompact = AccountTokens.Compact(BankCode);
            if (AccountTokens.IsBic(bankCompact))
                Bic = bankCompact;
        }

        /// <summary>Original :25: value, trimmed.</summary>
        public string Raw { get; }

        /// <summary>Text before the first slash, such as a BLZ or BIC. Null when there is no slash.</summary>
        public string BankCode { get; }

        /// <summary>Text after the slash, or the whole value when there is no slash.</summary>
        public string AccountNumber { get; }

        /// <summary>Account number when it is an IBAN, otherwise null. Spaces are removed.</summary>
        public string Iban { get; }

        /// <summary>Bank code when it is a BIC, otherwise null.</summary>
        public string Bic { get; }

        /// <summary>Returns <see cref="Raw"/>.</summary>
        public override string ToString()
        {
            return Raw;
        }

        private static string EmptyToNull(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
