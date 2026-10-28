namespace Mt940Parser
{
    /// <summary>
    /// Which MT940 balance tag a <see cref="Balance"/> came from.
    /// </summary>
    public enum BalanceKind
    {
        /// <summary>Opening balance, :60F:.</summary>
        Opening = 0,

        /// <summary>Intermediate opening balance, :60M:.</summary>
        IntermediateOpening = 1,

        /// <summary>Closing balance, :62F:.</summary>
        Closing = 2,

        /// <summary>Intermediate closing balance, :62M:.</summary>
        IntermediateClosing = 3,

        /// <summary>Closing available balance, :64:.</summary>
        ClosingAvailable = 4,

        /// <summary>Forward available balance, :65:.</summary>
        ForwardAvailable = 5
    }
}
