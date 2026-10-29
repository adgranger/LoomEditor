using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Snippet;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Suggest.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

// Monaco: SuggestController._insertSuggestion, the snippet branch (no Monaco test runs it without the widget).
public sealed class SuggestControllerSnippetTests : IDisposable
{
    private readonly SnippetFeature _feature = new();
    private readonly List<SuggestTestEditor> _editors = [];

    public void Dispose()
    {
        _feature.Dispose();
        foreach (SuggestTestEditor editor in _editors)
        {
            editor.Dispose();
        }
    }

    private SuggestTestEditor Editor(string text)
    {
        var editor = new SuggestTestEditor(text);
        _editors.Add(editor);
        _feature.Attach(editor.Context);
        return editor;
    }

    private static SuggestItem Item(string label, Position position, Range range, string insertText, InsertTextRules rules, params TextEdit[] additionalEdits)
    {
        var completion = new CompletionItem(label, insertText, CompletionItemKind.Snippet)
        {
            Range = CompletionRange.FromRange(range),
            InsertTextRules = rules,
            AdditionalTextEdits = additionalEdits.Length == 0 ? null : additionalEdits,
        };
        return new SuggestItem(position, completion, new CompletionList([completion], false), new FakeCompletionProvider());
    }

    private static SuggestController Controller(SuggestTestEditor editor) => new(editor.Context, SuggestModelOptions.Default);

    [Fact]
    public void Accept_SnippetItem_InsertsTemplateText_AndEntersSnippetMode()
    {
        SuggestTestEditor editor = Editor("fo");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 3);

        controller.Accept(Item("fn", new Position(1, 3), new Range(1, 1, 1, 3), "fn(${1:a}, ${2:b})$0", InsertTextRules.InsertAsSnippet));

        Assert.Equal("fn(a, b)", editor.Model.GetValue());
        Assert.True(editor.Context.InSnippetMode);
        Assert.Equal(new Selection(1, 4, 1, 5), editor.Context.Selection);
    }

    [Fact]
    public void Accept_SnippetItem_MultiCursor_AllGetSnippet()
    {
        SuggestTestEditor editor = Editor("a\nb");
        using SuggestController controller = Controller(editor);
        editor.Context.SetSelections([new Selection(1, 2, 1, 2), new Selection(2, 2, 2, 2)]);

        controller.Accept(Item("x", new Position(1, 2), new Range(1, 2, 1, 2), "[${1:x}]$0", InsertTextRules.InsertAsSnippet));

        Assert.Equal("a[x]\nb[x]", editor.Model.GetValue());
        Assert.Equal(2, editor.Context.Selections.Count);
    }

    [Fact]
    public void Accept_SnippetItem_ReplacesTypedPrefix()
    {
        SuggestTestEditor editor = Editor("let v = fo");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 11);

        controller.Accept(Item("for", new Position(1, 11), new Range(1, 9, 1, 11), "for ($1)$0", InsertTextRules.InsertAsSnippet));

        Assert.Equal("let v = for ()", editor.Model.GetValue());
        Assert.Equal(new Selection(1, 14, 1, 14), editor.Context.Selection);
    }

    [Fact]
    public void Accept_SnippetItem_KeepWhitespace_DoesNotReindent()
    {
        SuggestTestEditor editor = Editor("    ");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 5);

        controller.Accept(Item("s", new Position(1, 5), new Range(1, 5, 1, 5), "a\nb$0", InsertTextRules.InsertAsSnippet | InsertTextRules.KeepWhitespace));
        Assert.Equal("    a\nb", editor.Model.GetValue());

        editor.Model.SetValue("    ");
        editor.MoveCaret(1, 5);
        controller.Accept(Item("s", new Position(1, 5), new Range(1, 5, 1, 5), "a\nb$0", InsertTextRules.InsertAsSnippet));
        Assert.Equal("    a\n    b", editor.Model.GetValue());
    }

    [Fact]
    public void Accept_SnippetItem_AdditionalTextEdits_Applied()
    {
        SuggestTestEditor editor = Editor("x\nfo");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(2, 3);

        controller.Accept(Item("fn", new Position(2, 3), new Range(2, 1, 2, 3), "fn($1)$0", InsertTextRules.InsertAsSnippet, new TextEdit(new Range(1, 1, 1, 1), "import;\n")));

        Assert.Equal("import;\nx\nfn()", editor.Model.GetValue());
    }

    [Fact]
    public void Accept_PlainItem_Unchanged()
    {
        SuggestTestEditor editor = Editor("fo");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 3);

        controller.Accept(Item("f$1", new Position(1, 3), new Range(1, 1, 1, 3), "f${1:a}$0", InsertTextRules.None));

        Assert.Equal("f${1:a}$0", editor.Model.GetValue());
        Assert.False(editor.Context.InSnippetMode);
    }

    [Fact]
    public void Accept_SnippetItem_OneUndoStep()
    {
        SuggestTestEditor editor = Editor("fo");
        using SuggestController controller = Controller(editor);
        editor.MoveCaret(1, 3);
        controller.Accept(Item("fn", new Position(1, 3), new Range(1, 1, 1, 3), "fn(${1:a})$0", InsertTextRules.InsertAsSnippet));
        Assert.Equal("fn(a)", editor.Model.GetValue());

        editor.Model.Undo();

        Assert.Equal("fo", editor.Model.GetValue());
    }
}
