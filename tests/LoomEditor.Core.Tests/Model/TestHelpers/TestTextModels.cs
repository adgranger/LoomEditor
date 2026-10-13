// Ported from microsoft/vscode: src/vs/editor/test/common/testTextModel.ts (helper createTextModel)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;

namespace LoomEditor.Core.Tests.Model.TestHelpers;

internal static class TestTextModels
{
    /// <summary>Monaco: <c>createTextModel(text)</c>; default options, LF as the default EOL.</summary>
    public static TextModel Create(string text) => new(text);
}
