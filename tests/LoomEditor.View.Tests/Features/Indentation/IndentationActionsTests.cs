using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Indentation;

namespace LoomEditor.View.Tests.Features.Indentation;

public class IndentationActionsTests
{
    private sealed class IndentationEditor : IDisposable
    {
        private readonly EditorFeatureHost _host;

        public IndentationEditor(string text, bool withRules = false)
        {
            Context = new EditorContext();
            if (withRules)
            {
                Context.RegisterLanguage(TestLanguages.TypeScript);
                TestLanguages.RegisterLanguageConfiguration(Context.Model.LanguageConfigurationRegistry, TestLanguages.TypeScript);
                Context.Model.Tokenization.SetLanguageId(TestLanguages.TypeScript);
            }

            Context.SetText(text);
            _host = new EditorFeatureHost(Context, [() => new IndentationFeature()]);
        }

        public EditorContext Context { get; }

        public string Text => Context.Model.GetValue();

        public void Run(string id) => Context.Commands.Execute(id);

        public void Dispose()
        {
            _host.Dispose();
            Context.Dispose();
        }
    }

    // Monaco: 'indentationToSpaces' / 'indentationToTabs' actions
    [Fact]
    public void IndentationToSpaces_TabIndentedDocument_ConvertsAndSetsInsertSpaces()
    {
        using var editor = new IndentationEditor("a\n\tb\n\t\tc");
        editor.Context.Model.UpdateOptions(new LoomEditor.Core.Model.TextModelUpdateOptions(InsertSpaces: false));

        editor.Run(IndentationCommands.IndentationToSpaces);

        Assert.Equal("a\n    b\n        c", editor.Text);
        Assert.True(editor.Context.Model.Options.InsertSpaces);
    }

    [Fact]
    public void IndentationToTabs_SpaceIndentedDocument_ConvertsAndClearsInsertSpaces()
    {
        using var editor = new IndentationEditor("a\n    b\n        c");

        editor.Run(IndentationCommands.IndentationToTabs);

        Assert.Equal("a\n\tb\n\t\tc", editor.Text);
        Assert.False(editor.Context.Model.Options.InsertSpaces);
    }

    [Fact]
    public void ChangeIndentationSize_DisplaySizeOnly_ChangesOnlyTheTabSize()
    {
        using var editor = new IndentationEditor("a");

        IndentationActions.ChangeIndentationSize(editor.Context.ViewModel, 2, insertSpaces: false, displaySizeOnly: true);

        Assert.Equal(2, editor.Context.Model.Options.TabSize);
        Assert.Equal(4, editor.Context.Model.Options.IndentSize);
        Assert.True(editor.Context.Model.Options.InsertSpaces);
    }

    [Fact]
    public void ChangeIndentationSize_Full_ChangesSizesAndInsertSpaces()
    {
        using var editor = new IndentationEditor("a");

        IndentationActions.ChangeIndentationSize(editor.Context.ViewModel, 2, insertSpaces: false, displaySizeOnly: false);

        Assert.Equal(2, editor.Context.Model.Options.TabSize);
        Assert.Equal(2, editor.Context.Model.Options.IndentSize);
        Assert.False(editor.Context.Model.Options.InsertSpaces);
    }

    [Fact]
    public void IndentUsingSpacesCommand_TabSize12_AppliesEight()
    {
        using var editor = new IndentationEditor("a");
        editor.Context.Model.UpdateOptions(new LoomEditor.Core.Model.TextModelUpdateOptions(TabSize: 12, InsertSpaces: false));

        editor.Run(IndentationCommands.IndentUsingSpaces);

        Assert.Equal(8, editor.Context.Model.Options.TabSize);
        Assert.Equal(8, editor.Context.Model.Options.IndentSize);
        Assert.True(editor.Context.Model.Options.InsertSpaces);
    }

    [Fact]
    public void IndentUsingTabsCommand_TabSizeFour_KeepsFourAndUsesTabs()
    {
        using var editor = new IndentationEditor("a");

        editor.Run(IndentationCommands.IndentUsingTabs);

        Assert.Equal(4, editor.Context.Model.Options.TabSize);
        Assert.False(editor.Context.Model.Options.InsertSpaces);
    }

    [Fact]
    public void ChangeTabDisplaySizeCommand_TabSizeThree_KeepsTheIndentSize()
    {
        using var editor = new IndentationEditor("a");
        editor.Context.Model.UpdateOptions(new LoomEditor.Core.Model.TextModelUpdateOptions(TabSize: 3));

        editor.Run(IndentationCommands.ChangeTabDisplaySize);

        Assert.Equal(3, editor.Context.Model.Options.TabSize);
        Assert.Equal(4, editor.Context.Model.Options.IndentSize);
    }

    [Fact]
    public void DetectIndentation_TwoSpaceDocument_SetsTabSizeTwoAndSpaces()
    {
        using var editor = new IndentationEditor("a\n  b\n    c\n  d\ne\n  f\n    g");
        editor.Context.Model.UpdateOptions(new LoomEditor.Core.Model.TextModelUpdateOptions(TabSize: 8, InsertSpaces: false));

        editor.Run(IndentationCommands.DetectIndentation);

        Assert.Equal(2, editor.Context.Model.Options.TabSize);
        Assert.True(editor.Context.Model.Options.InsertSpaces);
    }

    [Fact]
    public void ReindentLines_JavaScriptRules_FixesTheWholeDocumentInOneUndoStep()
    {
        using var editor = new IndentationEditor("function f() {\nfoo();\nbar();\n}", withRules: true);

        editor.Run(IndentationCommands.ReindentLines);
        Assert.Equal("function f() {\n    foo();\n    bar();\n}", editor.Text);

        editor.Context.Model.Undo();
        Assert.Equal("function f() {\nfoo();\nbar();\n}", editor.Text);
    }

    [Fact]
    public void ReindentSelectedLines_SelectionEndingAtColumnOne_IgnoresTheLastLine()
    {
        using var editor = new IndentationEditor("function f() {\nfoo();\nbar();\n}", withRules: true);
        // Lines 2 and 3 selected, the selection ends at column 1 of line 3.
        editor.Context.Selection = new LoomEditor.Core.Text.Selection(2, 1, 3, 1);

        editor.Run(IndentationCommands.ReindentSelectedLines);

        Assert.Equal("function f() {\n    foo();\nbar();\n}", editor.Text);
    }

    [Fact]
    public void ReindentLines_ReadOnly_DoesNotRun()
    {
        using var editor = new IndentationEditor("function f() {\nfoo();\n}", withRules: true);
        editor.Context.ViewModel.SetCursorOptions(new EditorCursorOptions { ReadOnly = true });

        bool ran = editor.Context.Commands.Execute(IndentationCommands.ReindentLines);

        Assert.False(ran);
        Assert.Equal("function f() {\nfoo();\n}", editor.Text);
    }
}
