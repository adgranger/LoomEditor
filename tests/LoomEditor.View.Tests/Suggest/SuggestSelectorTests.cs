// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestMemory.ts (Memory.select)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Suggest.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

public class SuggestSelectorTests
{
    private static SuggestItem Make(string label, int score, bool preselect = false)
    {
        SuggestItem item = TestSuggestItems.CreateSuggestItem(label, 0);
        item.Score = [score, 0];
        item.Completion.Preselect = preselect;
        return item;
    }

    // Monaco: Memory.select
    [Fact]
    public void Select_Empty_Zero() => Assert.Equal(0, SuggestSelector.SelectInitialIndex([]));

    // Monaco: Memory.select
    [Fact]
    public void Select_NoPreselect_Zero() =>
        Assert.Equal(0, SuggestSelector.SelectInitialIndex([Make("a", 5), Make("b", 5), Make("c", 3)]));

    // Monaco: Memory.select
    [Fact]
    public void Select_PreselectInTopGroup_ItsIndex() =>
        Assert.Equal(1, SuggestSelector.SelectInitialIndex([Make("a", 5), Make("b", 5, preselect: true), Make("c", 3)]));

    // Monaco: Memory.select
    [Fact]
    public void Select_PreselectOutsideTopGroup_Zero() =>
        Assert.Equal(0, SuggestSelector.SelectInitialIndex([Make("a", 5), Make("b", 3, preselect: true)]));

    // Monaco: Memory.select
    [Fact]
    public void Select_FirstOfTopGroupPreselect_Zero() =>
        Assert.Equal(0, SuggestSelector.SelectInitialIndex([Make("a", 5, preselect: true), Make("b", 5, preselect: true)]));
}
