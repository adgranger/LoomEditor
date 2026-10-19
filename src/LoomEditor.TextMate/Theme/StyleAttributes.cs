// Ported from microsoft/vscode-textmate: src/theme.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Theme;

/// <summary>
/// <c>StyleAttributes</c>: the resolved style of a scope path. <c>fontFamily</c>, <c>fontSize</c> and
/// <c>lineHeight</c> are not ported (ADR 0008, decision 3).
/// </summary>
/// <param name="FontStyle"><c>fontStyle</c>.</param>
/// <param name="ForegroundId"><c>foregroundId</c>: an id of the theme's <see cref="ColorMap"/>.</param>
/// <param name="BackgroundId"><c>backgroundId</c>: an id of the theme's <see cref="ColorMap"/>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record StyleAttributes(FontStyle FontStyle, int ForegroundId, int BackgroundId);
