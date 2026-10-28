using System;

namespace Mt940Parser
{
    /// <summary>
    /// Statement number and optional sequence from :28: or :28C:.
    /// Leading zeros in <see cref="Number"/> and <see cref="Sequence"/> are preserved.
    /// </summary>
    public sealed class StatementNumber
    {
        /// <summary>
        /// Creates a statement number. <paramref name="number"/> and <paramref name="sequence"/> may be null
        /// when the raw value could not be split.
        /// </summary>
        public StatementNumber(string raw, string number, string sequence, string sourceTag)
        {
            Raw = raw ?? string.Empty;
            Number = string.IsNullOrEmpty(number) ? null : number;
            Sequence = string.IsNullOrEmpty(sequence) ? null : sequence;
            SourceTag = sourceTag ?? string.Empty;
            NumberValue = ParseInt(Number);
            SequenceValue = ParseInt(Sequence);
        }

        /// <summary>Original tag value.</summary>
        public string Raw { get; }

        /// <summary>Statement number text, including leading zeros. Null when the value is not numeric.</summary>
        public string Number { get; }

        /// <summary>Sequence text after the slash, including leading zeros. Null when omitted.</summary>
        public string Sequence { get; }

        /// <summary>Numeric statement number, or null when <see cref="Number"/> is absent.</summary>
        public int? NumberValue { get; }

        /// <summary>Numeric sequence, or null when <see cref="Sequence"/> is absent.</summary>
        public int? SequenceValue { get; }

        /// <summary>":28:" or ":28C:".</summary>
        public string SourceTag { get; }

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
