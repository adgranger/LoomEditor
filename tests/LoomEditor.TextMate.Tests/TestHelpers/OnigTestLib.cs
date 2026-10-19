// Ported from microsoft/vscode-textmate: src/tests/onigLibs.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Tests.TestHelpers;

/// <summary>The regex engine used by the tests (<c>getOniguruma</c>): the native Oniguruma engine.</summary>
internal static class OnigTestLib
{
    private static readonly OnigurumaEngine Engine = new();

    /// <summary>Returns the engine; synchronous (ADR 0008, decision 3).</summary>
    public static IOnigLib GetOniguruma() => Engine;
}
