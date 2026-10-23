// Ported from microsoft/vscode: src/vs/editor/test/browser/viewModel/viewModelDecorations.test.ts (suite 'ViewModelDecorations')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// The tests without a Monaco comment are new.
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

    private static void WithWrapped(Action<TextModel, ViewModel> callback) => TestCodeEditor.WithTestCodeEditor(
        "hello world, this is a buffer that will be wrapped",
        editor => callback(editor.Model, editor.ViewModel),
        new TestCodeEditorOptions(Wrapping: new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 13 }, EditorId: 1));

    private static void AssertWrapped(ViewModel viewModel)
    {
        Assert.Equal("hello world, ", viewModel.GetLineContent(1));
        Assert.Equal("this is a ", viewModel.GetLineContent(2));
        Assert.Equal("buffer that ", viewModel.GetLineContent(3));
        Assert.Equal("will be ", viewModel.GetLineContent(4));
        Assert.Equal("wrapped", viewModel.GetLineContent(5));
    }

    private static ModelDecorationOptions IdOptions(string id) => Options(new ModelDecorationOptionsData
    {
        Description = "test",
        ClassName = id,
        InlineClassName = "i-" + id,
        BeforeContentClassName = "b-" + id,
        AfterContentClassName = "a-" + id,
    });

    private static InlineDecoration Dec(int l1, int c1, int l2, int c2, string cls, InlineDecorationType type) => new(new Range(l1, c1, l2, c2), cls, type);

    // Monaco: 'getDecorationsViewportData'
    [Fact]
    public void GetDecorationsViewportData()
    {
        WithWrapped((model, viewModel) =>
        {
            AssertWrapped(viewModel);

            (int StartColumn, int EndColumn)[] ranges =
            [
                (2, 3), (2, 14), (2, 15), (2, 36), (2, 51),
                (14, 14), (14, 16), (14, 36), (14, 51),
                (16, 18), (16, 36), (16, 51),
                (36, 36), (36, 51),
                (40, 51),
            ];
            model.ChangeDecorations(0, accessor =>
            {
                for (int i = 0; i < ranges.Length; i++)
                {
                    accessor.AddDecoration(new Range(1, ranges[i].StartColumn, 1, ranges[i].EndColumn), IdOptions("dec" + (i + 1)));
                }
            });

            IReadOnlyList<ViewModelDecoration> decorations = viewModel.GetDecorationsInViewport(
                new Range(2, viewModel.GetLineMinColumn(2), 3, viewModel.GetLineMaxColumn(3)));
            Assert.Equal(
                Enumerable.Range(1, 14).Select(i => "dec" + i),
                decorations.Select(d => d.Options.ClassName).Where(c => !string.IsNullOrEmpty(c)));

            var visible1 = new Range(1, viewModel.GetLineMinColumn(1), 2, viewModel.GetLineMaxColumn(2));
            Assert.Equal(
                [
                    Dec(1, 2, 1, 3, "i-dec1", InlineDecorationType.Regular),
                    Dec(1, 2, 1, 2, "b-dec1", InlineDecorationType.Before),
                    Dec(1, 3, 1, 3, "a-dec1", InlineDecorationType.After),
                    Dec(1, 2, 1, 14, "i-dec2", InlineDecorationType.Regular),
                    Dec(1, 2, 1, 2, "b-dec2", InlineDecorationType.Before),
                    Dec(1, 14, 1, 14, "a-dec2", InlineDecorationType.After),
                    Dec(1, 2, 2, 2, "i-dec3", InlineDecorationType.Regular),
                    Dec(1, 2, 1, 2, "b-dec3", InlineDecorationType.Before),
                    Dec(1, 2, 3, 13, "i-dec4", InlineDecorationType.Regular),
                    Dec(1, 2, 1, 2, "b-dec4", InlineDecorationType.Before),
                    Dec(1, 2, 5, 8, "i-dec5", InlineDecorationType.Regular),
                    Dec(1, 2, 1, 2, "b-dec5", InlineDecorationType.Before),
                ],
                viewModel.GetViewportInlineDecorations(visible1, 1));

            var visible2 = new Range(2, viewModel.GetLineMinColumn(2), 3, viewModel.GetLineMaxColumn(3));
            Assert.Equal(
                [
                    Dec(1, 2, 2, 2, "i-dec3", InlineDecorationType.Regular),
                    Dec(2, 2, 2, 2, "a-dec3", InlineDecorationType.After),
                    Dec(1, 2, 3, 13, "i-dec4", InlineDecorationType.Regular),
                    Dec(1, 2, 5, 8, "i-dec5", InlineDecorationType.Regular),
                    Dec(2, 1, 2, 1, "i-dec6", InlineDecorationType.Regular),
                    Dec(2, 1, 2, 1, "b-dec6", InlineDecorationType.Before),
                    Dec(2, 1, 2, 1, "a-dec6", InlineDecorationType.After),
                    Dec(2, 1, 2, 3, "i-dec7", InlineDecorationType.Regular),
                    Dec(2, 1, 2, 1, "b-dec7", InlineDecorationType.Before),
                    Dec(2, 3, 2, 3, "a-dec7", InlineDecorationType.After),
                    Dec(2, 1, 3, 13, "i-dec8", InlineDecorationType.Regular),
                    Dec(2, 1, 2, 1, "b-dec8", InlineDecorationType.Before),
                    Dec(2, 1, 5, 8, "i-dec9", InlineDecorationType.Regular),
                    Dec(2, 1, 2, 1, "b-dec9", InlineDecorationType.Before),
                    Dec(2, 3, 2, 5, "i-dec10", InlineDecorationType.Regular),
                    Dec(2, 3, 2, 3, "b-dec10", InlineDecorationType.Before),
                    Dec(2, 5, 2, 5, "a-dec10", InlineDecorationType.After),
                    Dec(2, 3, 3, 13, "i-dec11", InlineDecorationType.Regular),
                    Dec(2, 3, 2, 3, "b-dec11", InlineDecorationType.Before),
                    Dec(2, 3, 5, 8, "i-dec12", InlineDecorationType.Regular),
                    Dec(2, 3, 2, 3, "b-dec12", InlineDecorationType.Before),
                ],
                viewModel.GetViewportInlineDecorations(visible2, 2));

            Assert.Equal(
                [
                    Dec(1, 2, 3, 13, "i-dec4", InlineDecorationType.Regular),
                    Dec(3, 13, 3, 13, "a-dec4", InlineDecorationType.After),
                    Dec(1, 2, 5, 8, "i-dec5", InlineDecorationType.Regular),
                    Dec(2, 1, 3, 13, "i-dec8", InlineDecorationType.Regular),
                    Dec(3, 13, 3, 13, "a-dec8", InlineDecorationType.After),
                    Dec(2, 1, 5, 8, "i-dec9", InlineDecorationType.Regular),
                    Dec(2, 3, 3, 13, "i-dec11", InlineDecorationType.Regular),
                    Dec(3, 13, 3, 13, "a-dec11", InlineDecorationType.After),
                    Dec(2, 3, 5, 8, "i-dec12", InlineDecorationType.Regular),
                ],
                viewModel.GetViewportInlineDecorations(visible2, 3));
        });
    }

    // Monaco: 'issue #17208: Problem scrolling in 1.8.0'
    [Fact]
    public void Issue17208ProblemScrollingIn180()
    {
        WithWrapped((model, viewModel) =>
        {
            AssertWrapped(viewModel);

            model.ChangeDecorations(0, accessor => accessor.AddDecoration(
                new Range(1, 50, 1, 51),
                Options(new ModelDecorationOptionsData { Description = "test", BeforeContentClassName = "dec1" })));
            var visible = new Range(2, viewModel.GetLineMinColumn(2), 3, viewModel.GetLineMaxColumn(3));

            Assert.DoesNotContain(viewModel.GetDecorationsInViewport(visible), d => !string.IsNullOrEmpty(d.Options.BeforeContentClassName));
            Assert.Empty(viewModel.GetViewportInlineDecorations(visible, 2));
            Assert.Empty(viewModel.GetViewportInlineDecorations(visible, 3));
        });
    }

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
