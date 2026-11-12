using System;

namespace Mt940Parser
{
    /// <summary>
    /// Statement number and optional sequence from :28: or :28C:.
    /// Leading zeros in <see cref="Number"/> and <see cref="Sequence"/> are preserved.
    /// </summary>
    public sealed class StatementNumber
    {
        private readonly string _raw;
        private readonly string _number;
        private readonly string _sequence;
        private readonly int? _numberValue;
        private readonly int? _sequenceValue;
        private readonly string _sourceTag;

        /// <summary>
        /// Creates a statement number. <paramref name="number"/> and <paramref name="sequence"/> may be null
        /// when the raw value could not be split.
        /// </summary>
        public StatementNumber(string raw, string number, string sequence, string sourceTag)
        {
            _raw = raw ?? string.Empty;
            _number = string.IsNullOrEmpty(number) ? null : number;
            _sequence = string.IsNullOrEmpty(sequence) ? null : sequence;
            _sourceTag = sourceTag ?? string.Empty;
            _numberValue = ParseInt(_number);
            _sequenceValue = ParseInt(_sequence);
        }

        /// <summary>Original tag value.</summary>
        public string Raw
        {
            get { return _raw; }
        }

        /// <summary>Statement number text, including leading zeros. Null when the value is not numeric.</summary>
        public string Number
        {
            get { return _number; }
        }

        /// <summary>Sequence text after the slash, including leading zeros. Null when omitted.</summary>
        public string Sequence
        {
            get { return _sequence; }
        }

        /// <summary>Numeric statement number, or null when <see cref="Number"/> is absent.</summary>
        public int? NumberValue
        {
            get { return _numberValue; }
        }

        /// <summary>Numeric sequence, or null when <see cref="Sequence"/> is absent.</summary>
        public int? SequenceValue
        {
            get { return _sequenceValue; }
        }

        /// <summary>":28:" or ":28C:".</summary>
        public string SourceTag
        {
            get { return _sourceTag; }
        }

        /// <summary>True when the tag was :28C:.</summary>
        public bool IsContinuationTag
        {
            get { return string.Equals(SourceTag, ":28C:", StringComparison.Ordinal); }
        }

        /// <summary>Returns the raw value.</summary>
        public override string ToString()
        {
            return Raw;
        }

        private static int? ParseInt(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            int value;
            if (int.TryParse(text, out value))
                return value;
            return null;
        }
    }
}
