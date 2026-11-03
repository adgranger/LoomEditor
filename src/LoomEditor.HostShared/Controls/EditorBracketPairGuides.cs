#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

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
