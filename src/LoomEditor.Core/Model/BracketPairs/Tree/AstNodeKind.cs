// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/ast.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>The kind of an <see cref="AstNode"/> (Monaco: <c>AstNodeKind</c>).</summary>
internal enum AstNodeKind
{
    Text = 0,
    Bracket = 1,
    Pair = 2,
    UnexpectedClosingBracket = 3,
    List = 4,
}
