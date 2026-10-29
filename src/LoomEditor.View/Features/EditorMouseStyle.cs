namespace LoomEditor.View.Features;

/// <summary>The mouse cursor over the text (Monaco: the <c>mouseStyle</c> option).</summary>
/// <remarks>Threading: immutable.</remarks>
internal enum EditorMouseStyle
{
    /// <summary>The text cursor.</summary>
    Text,

    /// <summary>The arrow, while a selection is dragged to be moved.</summary>
    Default,

    /// <summary>The copy cursor, while a selection is dragged to be copied.</summary>
    Copy,
}
