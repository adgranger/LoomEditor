// Ported from microsoft/vscode: src/vs/editor/contrib/smartSelect/browser/smartSelect.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.SmartSelect;

/// <summary>Monaco: <c>smartSelect.selectLeadingAndTrailingWhitespace</c> and <c>smartSelect.selectSubwords</c>.</summary>
/// <param name="SelectLeadingAndTrailingWhitespace">Whether a range that wraps onto new lines is first offered without, then with, its leading and trailing whitespace.</param>
/// <param name="SelectSubwords">Whether <c>fooBar</c>, <c>foo_bar</c> and <c>foo-bar</c> are first offered one part at a time.</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct SmartSelectOptions(bool SelectLeadingAndTrailingWhitespace = true, bool SelectSubwords = true);
