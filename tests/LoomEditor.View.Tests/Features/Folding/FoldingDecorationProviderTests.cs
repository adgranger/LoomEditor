// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/foldingDecorations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Features.Folding;

// New tests: foldingDecorations.ts has no upstream test.
public class FoldingDecorationProviderTests
{
    private const int LineHeight = 20;

    private readonly FakeTimeProvider _clock = new();
    private readonly FakeFrameScheduler _frames = new();

    private ViewModel CreateViewModel(TextModel model, int editorId = 0)
    {
        var fontInfo = new FontInfo(1, "Consolas", "normal", 14, LineHeight, 0, EditorFontOptions.LigaturesOff, EditorFontOptions.VariationsOff, true, 8.4, 16.8, true, 8.4, 8, 8, 8.4);
        return new ViewModel(model, fontInfo, new EditorLayoutOptions(), new ViewLayoutOptions(LineHeight), _frames.Schedule, _clock, editorId);
    }

    public static TheoryData<bool, bool, bool, int, bool> Matrix()
    {
        var data = new TheoryData<bool, bool, bool, int, bool>();
        foreach (bool isCollapsed in new[] { false, true })
        {
            foreach (bool isHidden in new[] { false, true })
            {
                foreach (bool isManual in new[] { false, true })
                {
                    foreach (ShowFoldingControls controls in Enum.GetValues<ShowFoldingControls>())
                    {
                        foreach (bool highlights in new[] { false, true })
                        {
                            data.Add(isCollapsed, isHidden, isManual, (int)controls, highlights);
                        }
                    }
                }
            }
        }

        return data;
    }

    // Monaco: foldingDecorations.ts getDecorationOption
    [Theory]
    [MemberData(nameof(Matrix))]
    public void GetDecorationOption_Matrix(bool isCollapsed, bool isHidden, bool isManual, int controlsValue, bool highlights)
    {
        var controls = (ShowFoldingControls)controlsValue;
        using ViewModel viewModel = CreateViewModel(new TextModel("a"));
        var provider = new FoldingDecorationProvider(viewModel) { ShowFoldingControls = controls, ShowFoldingHighlights = highlights };

        ModelDecorationOptions options = provider.GetDecorationOption(isCollapsed, isHidden, isManual);

        if (isHidden)
        {
            Assert.Equal("folding-hidden-range-decoration", options.Description);
            return;
        }

        string? icon = (isCollapsed, isManual, controls) switch
        {
            (_, _, ShowFoldingControls.Never) => null,
            (true, false, _) => "codicon codicon-folding-collapsed",
            (true, true, _) => "codicon codicon-folding-manual-collapsed",
            (false, false, ShowFoldingControls.Always) => "alwaysShowFoldIcons codicon codicon-folding-expanded",
            (false, false, _) => "codicon codicon-folding-expanded",
            (false, true, ShowFoldingControls.Always) => "alwaysShowFoldIcons codicon codicon-folding-manual-expanded",
            (false, true, _) => "codicon codicon-folding-manual-expanded",
        };
        Assert.Equal(icon, options.FirstLineDecorationClassName);
        Assert.Equal(isCollapsed ? "inline-folded" : null, options.AfterContentClassName);
        Assert.Equal(isCollapsed && highlights ? "folded-background" : null, options.ClassName);
        Assert.True(options.IsWholeLine);
    }

    [Fact]
    public void ChangeDecorations_UsesTheEditorId()
    {
        var model = new TextModel("a");
        using ViewModel viewModel = CreateViewModel(model, editorId: 7);
        var provider = new FoldingDecorationProvider(viewModel);

        string[] ids = provider.ChangeDecorations(accessor => accessor.DeltaDecorations([],
            [new ModelDeltaDecoration(new Range(1, 1, 1, 2), provider.GetDecorationOption(false, false, false))]));

        Assert.Equal(ids, model.GetAllDecorations().Where(d => d.OwnerId == 7).Select(d => d.Id));

        provider.RemoveDecorations(ids);

        Assert.DoesNotContain(model.GetAllDecorations(), d => d.OwnerId == 7);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CollapsedRegion_HasTheMinimapColor(bool highlights)
    {
        using ViewModel viewModel = CreateViewModel(new TextModel("a"));
        var provider = new FoldingDecorationProvider(viewModel) { ShowFoldingHighlights = highlights };

        ModelDecorationMinimapOptions? minimap = provider.GetDecorationOption(true, false, false).Minimap;

        Assert.Equal(highlights, minimap is not null);
        Assert.Equal(highlights ? "editor.foldBackground" : null, minimap?.Color!.ThemeColorId);
    }
}
