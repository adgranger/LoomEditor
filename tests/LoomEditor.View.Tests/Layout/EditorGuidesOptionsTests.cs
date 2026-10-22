// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (GuideOptions defaults)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class EditorGuidesOptionsTests
{
    [Fact]
    public void EditorGuidesOptions_Defaults_MatchMonaco()
    {
        EditorGuidesOptions o = new();

        Assert.Equal(GuideMode.Off, o.BracketPairs);
        Assert.Equal(GuideMode.Active, o.BracketPairsHorizontal);
        Assert.True(o.HighlightActiveBracketPair);
        Assert.True(o.Indentation);
        Assert.Equal(ActiveIndentationHighlight.On, o.HighlightActiveIndentation);
        Assert.Equal(o, new EditorLayoutOptions().Guides);
    }
}
