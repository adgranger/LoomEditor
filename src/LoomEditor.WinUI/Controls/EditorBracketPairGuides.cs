namespace LoomEditor.WinUI.Controls;

/// <summary>Which bracket pair guides are drawn (Monaco: <c>guides.bracketPairs</c>).</summary>
public enum EditorBracketPairGuides
{
    /// <summary>No bracket pair guides.</summary>
    Off,

    /// <summary>Guides for every bracket pair.</summary>
    On,

    /// <summary>Guides for the bracket pair around the cursor only.</summary>
    Active,
}
