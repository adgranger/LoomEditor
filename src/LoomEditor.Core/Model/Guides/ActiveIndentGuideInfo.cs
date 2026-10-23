// Ported from microsoft/vscode: src/vs/editor/common/textModelGuides.ts (IActiveIndentGuideInfo)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Guides;

/// <summary>The line range and the indent of the active indent guide.</summary>
/// <param name="StartLineNumber">The first line of the guide.</param>
/// <param name="EndLineNumber">The last line of the guide.</param>
/// <param name="Indent">The indent level of the guide.</param>
/// <remarks>Threading: immutable value type.</remarks>
internal readonly record struct ActiveIndentGuideInfo(int StartLineNumber, int EndLineNumber, int Indent);
