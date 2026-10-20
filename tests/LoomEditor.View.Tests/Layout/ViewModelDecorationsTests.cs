// Ported from microsoft/vscode: src/vs/editor/test/browser/viewModel/viewModelDecorations.test.ts (suite 'ViewModelDecorations')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Not ported: 'getDecorationsViewportData' and 'issue #17208: Problem scrolling in 1.8.0' (word wrap: phase 6).
// The other tests are new.
public class ViewModelDecorationsTests
{
    private const int LineHeight = 20;

    private readonly FakeTimeProvider _clock = new();
    private readonly FakeFrameScheduler _frames = new();

    private sealed class RecordingHandler : IViewEventHandler
    {
        public List<ViewEvent> Events { get; } = [];

        public void HandleEvents(IReadOnlyList<ViewEvent> events) => Events.AddRange(events);
    }

    // The whole line is one comment token.
    private sealed class CommentSupport : ITokenizationSupport
    {
        private const uint CommentMeta = (uint)StandardTokenType.Comment << MetadataConsts.TokenTypeOffset;

        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) => new([0, CommentMeta], NullState.Instance);
    }

    private ViewModel CreateViewModel(TextModel model, int editorId = 0)
    {
        var fontInfo = new FontInfo(1, "Consolas", "normal", 14, LineHeight, 0, true, 8.4, 16.8, true, 8.4, 8, 8, 8.4);
        var viewModel = new ViewModel(model, fontInfo, new EditorLayoutOptions(), new ViewLayoutOptions(LineHeight), _frames.Schedule, _clock, editorId);
        viewModel.SetViewportSize(400, 100);
        return viewModel;
    }

    private static ModelDecorationOptions Options(ModelDecorationOptionsData data) => ModelDecorationOptions.CreateDynamic(data);

    [Fact]
    public void Issue37401_AllowBothBeforeAndAfterDecorationsOnEmptyLine()
    {
        // Monaco: 'issue #37401: Allow both before and after decorations on empty line'
        var model = new TextModel(string.Empty);
        using ViewModel viewModel = CreateViewModel(model);

        model.ChangeDecorations(0, accessor => accessor.AddDecoration(
            new Range(1, 1, 1, 1),
            Options(new ModelDecorationOptionsData { Description = "test", BeforeContentClassName = "before1", AfterContentClassName = "after1" })));

        IReadOnlyList<InlineDecoration> inlineDecorations = viewModel.GetViewportInlineDecorations(new Range(1, 1, 1, 1), 1);

        Assert.Equal(
            [
                new InlineDecoration(new Range(1, 1, 1, 1), "before1", InlineDecorationType.Before),
                new InlineDecoration(new Range(1, 1, 1, 1), "after1", InlineDecorationType.After),
            ],
            inlineDecorations);
    }

    [Fact]
    public void DecorationsChanged_EmitsViewDecorationsChangedEvent()
    {
        var model = new TextModel("hello");
        using ViewModel viewModel = CreateViewModel(model);
        var handler = new RecordingHandler();
        viewModel.EventDispatcher.AddViewEventHandler(handler);

        model.DeltaDecorations(0, [], [new ModelDeltaDecoration(new Range(1, 1, 1, 3), Options(new ModelDecorationOptionsData { Description = "test", GlyphMarginClassName = "glyph" }))]);

        ViewDecorationsChangedEvent e = Assert.IsType<ViewDecorationsChangedEvent>(Assert.Single(handler.Events));
        Assert.True(e.AffectsGlyphMargin);
        Assert.False(e.AffectsLineNumber);
    }

    [Fact]
    public void GetDecorationsInViewport_OtherEditorsDecorations_AreHidden()
    {
        var model = new TextModel("hello");
        using ViewModel viewModel = CreateViewModel(model, editorId: 1);
        var options = Options(new ModelDecorationOptionsData { Description = "test", ClassName = "x" });
        model.DeltaDecorations(1, [], [new ModelDeltaDecoration(new Range(1, 1, 1, 3), options)]);
        model.DeltaDecorations(2, [], [new ModelDeltaDecoration(new Range(1, 2, 1, 4), options)]);

        IReadOnlyList<ViewModelDecoration> decorations = viewModel.GetDecorationsInViewport(new Range(1, 1, 1, 6));

        Assert.Equal(new Range(1, 1, 1, 3), Assert.Single(decorations, d => d.Options.ClassName == "x").Range);
    }

    [Fact]
    public void GetDecorationsInViewport_HideInCommentTokens_HidesDecorationInAComment()
    {
        var registry = new TokenizationRegistry();
        registry.Register("comment", new CommentSupport());
        var codec = new LanguageIdCodec();
        codec.Register("comment");
        var model = new TextModel("// hello", DefaultEndOfLine.LF, null, registry, codec, null);
        model.Tokenization.SetLanguageId("comment");
        model.Tokenization.ForceTokenization(model.LineCount);
        using ViewModel viewModel = CreateViewModel(model);
        model.DeltaDecorations(0, [], [
            new ModelDeltaDecoration(new Range(1, 4, 1, 9), Options(new ModelDecorationOptionsData { Description = "test", ClassName = "hidden", HideInCommentTokens = true })),
            new ModelDeltaDecoration(new Range(1, 4, 1, 9), Options(new ModelDecorationOptionsData { Description = "test", ClassName = "shown" })),
        ]);

        IReadOnlyList<ViewModelDecoration> decorations = viewModel.GetDecorationsInViewport(new Range(1, 1, 1, 10));

        // The tracked range of the cursor is also a decoration (owner 0): look at the named ones only.
        Assert.Equal("shown", Assert.Single(decorations, d => d.Options.ClassName is not null).Options.ClassName);
    }

    [Fact]
    public void SetValue_ResetsTheCache()
    {
        var model = new TextModel("hello");
        using ViewModel viewModel = CreateViewModel(model);
        model.DeltaDecorations(0, [], [new ModelDeltaDecoration(new Range(1, 1, 1, 3), Options(new ModelDecorationOptionsData { Description = "test", ClassName = "x" }))]);
        var range = new Range(1, 1, 1, 6);
        Assert.Single(viewModel.GetDecorationsInViewport(range), d => d.Options.ClassName == "x");

        model.SetValue("hello");

        Assert.DoesNotContain(viewModel.GetDecorationsInViewport(range), d => d.Options.ClassName == "x");
    }

    [Fact]
    public void ViewportData_GetInlineDecorations_ReturnsTheLinesInlineDecorations()
    {
        var model = new TextModel("hello\nworld");
        using ViewModel viewModel = CreateViewModel(model);
        model.ChangeDecorations(0, accessor =>
        {
            accessor.AddDecoration(new Range(1, 1, 1, 3), Options(new ModelDecorationOptionsData { Description = "test", InlineClassName = "first" }));
            accessor.AddDecoration(new Range(2, 2, 2, 4), Options(new ModelDecorationOptionsData { Description = "test", InlineClassName = "second" }));
        });

        ViewportData data = viewModel.GetViewportData([Selection.Collapsed(new Position(1, 1))]);

        Assert.Equal(2, data.GetDecorationsInViewport().Count(d => d.Options.InlineClassName is not null));
        Assert.Equal([new InlineDecoration(new Range(1, 1, 1, 3), "first", InlineDecorationType.Regular)], data.GetInlineDecorations(1));
        Assert.Equal([new InlineDecoration(new Range(2, 2, 2, 4), "second", InlineDecorationType.Regular)], data.GetInlineDecorations(2));
    }
}
