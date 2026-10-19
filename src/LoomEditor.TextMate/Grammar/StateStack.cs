// Ported from microsoft/vscode-textmate: src/main.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>StateStack</c>: the state of the tokenizer at the end of a line. <b>Immutable.</b></summary>
/// <remarks>Threading: immutable, so safe to share (see <see cref="StateStackImpl"/> for the one exception).</remarks>
internal interface StateStack
{
    /// <summary><c>INITIAL</c>: the state before the first line.</summary>
    static StateStack Initial => StateStackImpl.Null;

    /// <summary><c>depth</c>: the depth of the stack.</summary>
    int Depth { get; }

    /// <summary><c>clone</c>.</summary>
    StateStack Clone();

    /// <summary><c>equals</c>: structural equality.</summary>
    bool Equals(StateStack? other);
}
