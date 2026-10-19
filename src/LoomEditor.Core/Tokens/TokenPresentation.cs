// Ported from microsoft/vscode: src/vs/editor/common/encodedTokenAttributes.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>What a token metadata says about how to draw it (Monaco: <c>ITokenPresentation</c>).</summary>
/// <param name="Foreground">The foreground color id.</param>
/// <param name="Italic">Italic.</param>
/// <param name="Bold">Bold.</param>
/// <param name="Underline">Underline.</param>
/// <param name="Strikethrough">Strikethrough.</param>
/// <remarks>Threading: a value type.</remarks>
internal readonly record struct TokenPresentation(int Foreground, bool Italic, bool Bold, bool Underline, bool Strikethrough);
