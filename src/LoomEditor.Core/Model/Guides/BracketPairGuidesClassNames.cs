// Ported from microsoft/vscode: src/vs/editor/common/model/guidesTextModelPart.ts (BracketPairGuidesClassNames)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;

namespace LoomEditor.Core.Model.Guides;

/// <summary>The class names of bracket pair guides.</summary>
/// <remarks>Threading: stateless, safe to call from any thread.</remarks>
internal static class BracketPairGuidesClassNames
{
    /// <summary>The class name of the active guide.</summary>
    public const string ActiveClassName = "indent-active";

    /// <summary>The class name of a guide at the given nesting levels.</summary>
    public static string GetInlineClassName(int nestingLevel, int nestingLevelOfEqualBracketType, bool independentColorPoolPerBracketType) =>
        GetInlineClassNameOfLevel(independentColorPoolPerBracketType ? nestingLevelOfEqualBracketType : nestingLevel);

    /// <summary>The class name of a guide at the given level.</summary>
    public static string GetInlineClassNameOfLevel(int level) =>
        // 30 is a lcm of all numbers from 1 to 6, to support a dynamic amount of colors up to 6 colors.
        string.Create(CultureInfo.InvariantCulture, $"bracket-indent-guide lvl-{level % 30}");
}
