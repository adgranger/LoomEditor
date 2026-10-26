using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Suggest.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

public class SuggestControllerAsyncEditsTests
{
    private sealed class PendingResolveProvider : ICompletionItemProvider
    {
        private readonly TaskCompletionSource<CompletionItem?> _resolve = new();

        public IReadOnlyList<string>? TriggerCharacters => null;

        public bool CanResolve { get; init; } = true;

        public int ResolveCalls { get; private set; }

        public CancellationToken ResolveToken { get; private set; }

        public Task<CompletionList?> ProvideCompletionItemsAsync(TextModel model, Position position, CompletionContext context, CancellationToken token) =>
            Task.FromResult<CompletionList?>(null);

        public Task<CompletionItem?> ResolveCompletionItemAsync(CompletionItem item, CancellationToken token)
        {
            ResolveCalls++;
            ResolveToken = token;
            return _resolve.Task;
        }

        public void Complete(params TextEdit[] edits) =>
            _resolve.SetResult(new CompletionItem("foo", "foo", CompletionItemKind.Property) { AdditionalTextEdits = edits.Length == 0 ? null : edits });

        public void Fail() => _resolve.SetException(new InvalidOperationException("resolve failed"));
    }

    private static readonly TextEdit UsingEdit = new(new Range(1, 1, 1, 1), "using X;\n");

    private static SuggestItem Item(PendingResolveProvider provider, int line, params TextEdit[] syncEdits)
    {
        var completion = new CompletionItem("foo", "foo", CompletionItemKind.Property)
        {
            Range = CompletionRange.FromRange(new Range(line, 13, line, 15)),
            AdditionalTextEdits = syncEdits.Length == 0 ? null : syncEdits,
        };
        return new SuggestItem(new Position(line, 15), completion, new CompletionList([completion], false), provider);
    }

    private static SuggestController Controller(SuggestTestEditor editor) => new(editor.Context, SuggestModelOptions.Default);

    [Fact]
    public void AsyncEdits_AppliedAfterResolve()
    {
        using var editor = new SuggestTestEditor("x\nlet value = fo");
        using SuggestController controller = Controller(editor);
        var provider = new PendingResolveProvider();
        editor.MoveCaret(2, 15);

        controller.Accept(Item(provider, 2));
        Assert.Equal("x\nlet value = foo", editor.Model.GetValue());
        provider.Complete(UsingEdit);
        editor.Flush();

        Assert.Equal("using X;\nx\nlet value = foo", editor.Model.GetValue());
    }

    [Fact]
    public void AsyncEdits_ChangeBeforeEditStart_Skipped()
    {
        using var editor = new SuggestTestEditor("abc\nlet value = fo");
        using SuggestController controller = Controller(editor);
        var provider = new PendingResolveProvider();
        editor.MoveCaret(2, 15);

        controller.Accept(Item(provider, 2));
        editor.MoveCaret(1, 1);
        editor.Type("q");
        provider.Complete(new TextEdit(new Range(1, 3, 1, 3), "Z"));
        editor.Flush();

        Assert.Equal("qabc\nlet value = foo", editor.Model.GetValue());
    }

    [Fact]
    public void AsyncEdits_TypingAfterTheEdit_Applied()
    {
        using var editor = new SuggestTestEditor("x\nlet value = fo");
        using SuggestController controller = Controller(editor);
        var provider = new PendingResolveProvider();
        editor.MoveCaret(2, 15);

        controller.Accept(Item(provider, 2));
        editor.Type(".");
        provider.Complete(UsingEdit);
        editor.Flush();

        Assert.Equal("using X;\nx\nlet value = foo.", editor.Model.GetValue());
    }

    [Fact]
    public void AsyncEdits_Flush_Cancelled()
    {
        using var editor = new SuggestTestEditor("x\nlet value = fo");
        using SuggestController controller = Controller(editor);
        var provider = new PendingResolveProvider();
        editor.MoveCaret(2, 15);

        controller.Accept(Item(provider, 2));
        editor.SetText("zzz");
        provider.Complete(UsingEdit);
        editor.Flush();

        Assert.Equal("zzz", editor.Model.GetValue());
        Assert.True(provider.ResolveToken.IsCancellationRequested);
    }

    [Fact]
    public void AsyncEdits_NoEdits_NothingApplied()
    {
        using var editor = new SuggestTestEditor("x\nlet value = fo");
        using SuggestController controller = Controller(editor);
        var provider = new PendingResolveProvider();
        editor.MoveCaret(2, 15);

        controller.Accept(Item(provider, 2));
        provider.Complete();
        editor.Flush();

        Assert.Equal("x\nlet value = foo", editor.Model.GetValue());
    }

    [Fact]
    public void AsyncEdits_ResolveFaulted_NothingApplied_NoLeak()
    {
        using var editor = new SuggestTestEditor("x\nlet value = fo");
        using SuggestController controller = Controller(editor);
        var provider = new PendingResolveProvider();
        editor.MoveCaret(2, 15);

        controller.Accept(Item(provider, 2));
        provider.Fail();
        editor.Flush();
        editor.Type(".");

        Assert.Equal("x\nlet value = foo.", editor.Model.GetValue());
    }

    // The accept has no undo stop after it while the edits are pending; typing pushes it. Undo steps: the late edits, the typing, the accept.
    [Fact]
    public void AsyncEdits_UndoStops()
    {
        using var editor = new SuggestTestEditor("x\nlet value = fo");
        using SuggestController controller = Controller(editor);
        var provider = new PendingResolveProvider();
        editor.MoveCaret(2, 15);

        controller.Accept(Item(provider, 2));
        editor.Type(".");
        provider.Complete(UsingEdit);
        editor.Flush();
        Assert.Equal("using X;\nx\nlet value = foo.", editor.Model.GetValue());

        editor.Model.Undo();
        Assert.Equal("x\nlet value = foo.", editor.Model.GetValue());
        editor.Model.Undo();
        Assert.Equal("x\nlet value = foo", editor.Model.GetValue());
        editor.Model.Undo();
        Assert.Equal("x\nlet value = fo", editor.Model.GetValue());
    }

    [Fact]
    public void AsyncEdits_NotUsedWhenAlreadyResolved()
    {
        using var editor = new SuggestTestEditor("x\nlet value = fo");
        using SuggestController controller = Controller(editor);
        var provider = new PendingResolveProvider { CanResolve = false };
        editor.MoveCaret(2, 15);

        controller.Accept(Item(provider, 2));
        editor.Flush();

        Assert.Equal(0, provider.ResolveCalls);
    }

    [Fact]
    public void AsyncEdits_NotUsedWhenSyncEditsExist()
    {
        using var editor = new SuggestTestEditor("x\nlet value = fo");
        using SuggestController controller = Controller(editor);
        var provider = new PendingResolveProvider();
        editor.MoveCaret(2, 15);

        controller.Accept(Item(provider, 2, UsingEdit));

        Assert.Equal(0, provider.ResolveCalls);
        Assert.Equal("using X;\nx\nlet value = foo", editor.Model.GetValue());
    }

    [Fact]
    public void AsyncEdits_Dispose_CancelsPending()
    {
        using var editor = new SuggestTestEditor("x\nlet value = fo");
        SuggestController controller = Controller(editor);
        var provider = new PendingResolveProvider();
        editor.MoveCaret(2, 15);
        controller.Accept(Item(provider, 2));

        controller.Dispose();
        provider.Complete(UsingEdit);
        editor.Flush();

        Assert.True(provider.ResolveToken.IsCancellationRequested);
        Assert.Equal("x\nlet value = foo", editor.Model.GetValue());
    }

    // ExecuteEdits stands for the next edit: unlike typing, it does not push an undo stop of its own.
    [Theory]
    [InlineData(true, "fo")]
    [InlineData(false, "foo")]
    public void Accept_NoAfterUndoStop_DoesNotPushTheFinalStop(bool noAfterUndoStop, string expectedAfterUndo)
    {
        using var editor = new SuggestTestEditor("fo");
        using SuggestController controller = Controller(editor);
        var resolved = new PendingResolveProvider { CanResolve = false };
        editor.MoveCaret(1, 3);

        controller.Accept(
            new SuggestItem(
                new Position(1, 3),
                new CompletionItem("foo", "foo", CompletionItemKind.Property) { Range = CompletionRange.FromRange(new Range(1, 1, 1, 3)) },
                new CompletionList([], false),
                resolved),
            noAfterUndoStop);
        editor.Context.ViewModel.ExecuteEdits("test", [new IdentifiedEditOperation(new Range(1, 4, 1, 4), "x")], _ => null);
        Assert.Equal("foox", editor.Model.GetValue());
        editor.Model.Undo();

        Assert.Equal(expectedAfterUndo, editor.Model.GetValue());
    }
}
