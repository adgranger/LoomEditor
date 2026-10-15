// Ported from microsoft/vscode: src/vs/editor/common/textModelEvents.ts (IModelOptionsChangedEvent)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>Tells which options of a <see cref="TextModel"/> changed.</summary>
/// <remarks>
/// <para>Monaco: <c>IModelOptionsChangedEvent</c>.</para>
/// <para>Threading: immutable; raised on the thread that updates the options.</para>
/// </remarks>
public sealed class ModelOptionsChangedEventArgs : EventArgs
{
    internal ModelOptionsChangedEventArgs(bool tabSize, bool indentSize, bool insertSpaces, bool trimAutoWhitespace)
    {
        TabSize = tabSize;
        IndentSize = indentSize;
        InsertSpaces = insertSpaces;
        TrimAutoWhitespace = trimAutoWhitespace;
    }

    /// <summary>True when the tab size changed.</summary>
    public bool TabSize { get; }

    /// <summary>True when the indent size changed.</summary>
    public bool IndentSize { get; }

    /// <summary>True when the insert-spaces option changed.</summary>
    public bool InsertSpaces { get; }

    /// <summary>True when the trim-auto-whitespace option changed.</summary>
    public bool TrimAutoWhitespace { get; }
}
