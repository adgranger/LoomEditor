// Ported from microsoft/vscode: src/vs/editor/common/model/intervalTree.ts (ClassName)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Markers;

/// <summary>The decoration class names of markers (Monaco <c>ClassName</c>); the view draws them (P8-06).</summary>
/// <remarks>Threading: constants; thread-safe.</remarks>
internal static class MarkerClassNames
{
    public const string EditorHintDecoration = "squiggly-hint";
    public const string EditorInfoDecoration = "squiggly-info";
    public const string EditorWarningDecoration = "squiggly-warning";
    public const string EditorErrorDecoration = "squiggly-error";
    public const string EditorUnnecessaryDecoration = "squiggly-unnecessary";
    public const string EditorUnnecessaryInlineDecoration = "squiggly-inline-unnecessary";
    public const string EditorDeprecatedInlineDecoration = "squiggly-inline-deprecated";
}
