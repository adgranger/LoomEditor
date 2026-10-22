namespace LoomEditor.WinUI.Controls;

/// <summary>How the continuation lines of a wrapped line are indented (Monaco: <c>wrappingIndent</c>).</summary>
public enum EditorWrappingIndent
{
    /// <summary>No indentation: wrapped lines begin at column 1.</summary>
    None,

    /// <summary>Same as the parent line.</summary>
    Same,

    /// <summary>The parent line's indentation plus one.</summary>
    Indent,

    /// <summary>The parent line's indentation plus two.</summary>
    DeepIndent,
}
