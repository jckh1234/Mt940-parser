using System.Collections.Generic;
using System.Text;

namespace Mt940Parser.Parsing
{
    internal sealed class StatementBuilder
    {
        private readonly IList<Mt940ParseException> _errors;
        private readonly List<Balance> _forward = new List<Balance>();
        private readonly List<Mt940Transaction> _transactions = new List<Mt940Transaction>();
        private readonly StringBuilder _statementInfo = new StringBuilder();
        private string _reference;
        private string _related;
        private AccountIdentification _account;
        private StatementNumber _number;
        private Balance _opening;
        private Balance _closing;
        private Balance _available;
        private bool _transactionOpen;
        private bool _hasContent;
        private string _currency;

        public StatementBuilder(IList<Mt940ParseException> errors)
        {
            _errors = errors;
        }

        public bool HasContent
        {
            get { return _hasContent; }
        }

        public void Apply(string tag, string content, int lineNumber)
        {
            switch (tag)
            {
                case "20":
                    ApplyReference(content, lineNumber);
                    return;
                case "21":
                    ApplyRelated(content, lineNumber);
                    return;
                case "25":
                    ApplyAccount(content, lineNumber);
                    return;
                case "28":
                case "28C":
                    ApplyNumber(tag, content, lineNumber);
                    return;
                case "60F":
                case "60M":
                    ApplyOpening(tag, content, lineNumber);
                    return;
                case "61":
                    ApplyTransaction(content, lineNumber);
                    return;
                case "86":
                    ApplyInformation(content);
                    return;
                case "62F":
                case "62M":
                    ApplyClosing(tag, content, lineNumber);
                    return;
                case "64":
                    ApplyAvailable(content, lineNumber);
                    return;
                case "65":
                    ApplyForward(content, lineNumber);
                    return;
                default:
                    return;
            }
        }

        public void Complete(List<Mt940Statement> statements)
        {
            if (!_hasContent)
                return;

            Mt940Information information = null;
            if (_statementInfo.Length > 0)
                information = Tag86Parser.Parse(_statementInfo.ToString());

            statements.Add(new Mt940Statement(
                _reference,
                _related,
                _account,
                _number,
                _opening,
                _closing,
                _available,
                _forward,
                _transactions,
                information));
            Reset();
        }

        private void ApplyReference(string content, int lineNumber)
        {
            CloseTransaction();
            _hasContent = true;
            var reference = SingleLine(content);
            if (reference.Length == 0)
            {
                _errors.Add(new Mt940ParseException("Transaction reference is empty.", lineNumber, ":20:"));
                return;
            }

            if (_reference != null)
            {
                _errors.Add(new Mt940ParseException("Duplicate transaction reference.", lineNumber, ":20:"));
                return;
            }

            _reference = reference;
        }

        private void ApplyRelated(string content, int lineNumber)
        {
            CloseTransaction();
            _hasContent = true;
            var related = SingleLine(content);
            if (related.Length == 0)
            {
                _errors.Add(new Mt940ParseException("Related reference is empty.", lineNumber, ":21:"));
                return;
            }

            if (_related != null)
            {
                _errors.Add(new Mt940ParseException("Duplicate related reference.", lineNumber, ":21:"));
                return;
            }

            _related = related;
        }

        private void ApplyAccount(string content, int lineNumber)
        {
            CloseTransaction();
            _hasContent = true;
            var raw = SingleLine(content);
            if (raw.Length == 0)
            {
                _errors.Add(new Mt940ParseException("Account identification is empty.", lineNumber, ":25:"));
                return;
            }

            if (_account != null)
            {
                _errors.Add(new Mt940ParseException("Duplicate account identification.", lineNumber, ":25:"));
                return;
            }

            _account = new AccountIdentification(raw);
        }

        private void ApplyNumber(string tag, string content, int lineNumber)
        {
            CloseTransaction();
            _hasContent = true;
            if (_number != null)
            {
                _errors.Add(new Mt940ParseException("Duplicate statement number.", lineNumber, ":" + tag + ":"));
                return;
            }

            _number = StatementNumberParser.Parse(content, tag, lineNumber, _errors);
        }

        private void ApplyOpening(string tag, string content, int lineNumber)
        {
            CloseTransaction();
            _hasContent = true;
            var sourceTag = ":" + tag + ":";
            if (_opening != null)
            {
                _errors.Add(new Mt940ParseException("Duplicate opening balance.", lineNumber, sourceTag));
                return;
            }

            Balance balance;
            if (!BalanceParser.TryParse(tag, content, lineNumber, _errors, out balance))
                return;

            _opening = balance;
            NoteCurrency(balance, lineNumber);
        }

        private void ApplyClosing(string tag, string content, int lineNumber)
        {
            CloseTransaction();
            _hasContent = true;
            var sourceTag = ":" + tag + ":";
            if (_closing != null)
            {
                _errors.Add(new Mt940ParseException("Duplicate closing balance.", lineNumber, sourceTag));
                return;
            }

            Balance balance;
            if (!BalanceParser.TryParse(tag, content, lineNumber, _errors, out balance))
                return;

            _closing = balance;
            NoteCurrency(balance, lineNumber);
        }

        private void ApplyAvailable(string content, int lineNumber)
        {
            CloseTransaction();
            _hasContent = true;
            if (_available != null)
            {
                _errors.Add(new Mt940ParseException("Duplicate closing available balance.", lineNumber, ":64:"));
                return;
            }

            Balance balance;
            if (!BalanceParser.TryParse("64", content, lineNumber, _errors, out balance))
                return;

            _available = balance;
            NoteCurrency(balance, lineNumber);
        }

        private void ApplyForward(string content, int lineNumber)
        {
            CloseTransaction();
            _hasContent = true;
            Balance balance;
            if (!BalanceParser.TryParse("65", content, lineNumber, _errors, out balance))
                return;

            _forward.Add(balance);
            NoteCurrency(balance, lineNumber);
        }

        private void ApplyTransaction(string content, int lineNumber)
        {
            CloseTransaction();
            _hasContent = true;
            Mt940Transaction transaction;
            if (!Tag61Parser.TryParse(content, lineNumber, _errors, out transaction))
                return;

            _transactions.Add(transaction);
            _transactionOpen = true;
        }

        private void ApplyInformation(string content)
        {
            _hasContent = true;
            if (_transactionOpen && _transactions.Count > 0)
            {
                var index = _transactions.Count - 1;
                var current = _transactions[index];
                var combined = current.Information == null
                    ? content
                    : current.Information.Raw + "\n" + content;
                _transactions[index] = current.WithInformation(Tag86Parser.Parse(combined));
                return;
            }

            if (_statementInfo.Length > 0)
                _statementInfo.Append('\n');
            _statementInfo.Append(content ?? string.Empty);
        }

        private void NoteCurrency(Balance balance, int lineNumber)
        {
            if (balance == null)
                return;
            if (_currency == null)
            {
                _currency = balance.Currency;
                return;
            }

            if (!string.Equals(_currency, balance.Currency, System.StringComparison.Ordinal))
            {
                _errors.Add(new Mt940ParseException(
                    "Currency " + balance.Currency + " does not match statement currency " + _currency + ".",
                    lineNumber,
                    balance.SourceTag));
            }
        }

        private void CloseTransaction()
        {
            _transactionOpen = false;
        }

        private void Reset()
        {
            _reference = null;
            _related = null;
            _account = null;
            _number = null;
            _opening = null;
            _closing = null;
            _available = null;
            _forward.Clear();
            _transactions.Clear();
            _statementInfo.Clear();
            _transactionOpen = false;
            _hasContent = false;
            _currency = null;
        }

        private static string SingleLine(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;
            return content.Replace("\n", string.Empty).Trim();
        }
    }
}
