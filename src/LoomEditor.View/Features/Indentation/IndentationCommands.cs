namespace LoomEditor.View.Features.Indentation;

/// <summary>Ids of the commands exposed by <see cref="IndentationFeature"/>. Same ids as in Monaco.</summary>
public static class IndentationCommands
{
    /// <summary>Converts the indentation of the document to spaces.</summary>
    public const string IndentationToSpaces = "editor.action.indentationToSpaces";

    /// <summary>Converts the indentation of the document to tabs.</summary>
    public const string IndentationToTabs = "editor.action.indentationToTabs";

    /// <summary>Indents using tabs.</summary>
    public const string IndentUsingTabs = "editor.action.indentUsingTabs";

    /// <summary>Indents using spaces.</summary>
    public const string IndentUsingSpaces = "editor.action.indentUsingSpaces";

    /// <summary>Changes the display size of a tab.</summary>
    public const string ChangeTabDisplaySize = "editor.action.changeTabDisplaySize";

    /// <summary>Detects the indentation of the document.</summary>
    public const string DetectIndentation = "editor.action.detectIndentation";

    /// <summary>Re-indents all the lines.</summary>
    public const string ReindentLines = "editor.action.reindentlines";

    /// <summary>Re-indents the selected lines.</summary>
    public const string ReindentSelectedLines = "editor.action.reindentselectedlines";
}
