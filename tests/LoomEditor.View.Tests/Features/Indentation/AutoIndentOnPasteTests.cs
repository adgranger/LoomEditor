using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Controller;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Indentation;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Indentation;

// Monaco: suite 'Auto Indent On Paste - TypeScript/JavaScript' (the five test.skip tests are not ported)
public class AutoIndentOnPasteTests
{
    private const string LanguageId = TestLanguages.TypeScript;

    private static StandardTokenTypeData[] Line(StandardTokenType type, params int[] startIndexes) =>
        [.. startIndexes.Select(startIndex => new StandardTokenTypeData(startIndex, type))];

    private static void Run(string initialText, StandardTokenTypeData[][]? tokens, Action<TestCodeEditor, AutoIndentOnPaste> test)
    {
        using var services = new TestEditorServices();
        services.RegisterLanguage(LanguageId);
        services.Add(TestLanguages.RegisterLanguageConfiguration(services.LanguageConfigurationRegistry, LanguageId));
        TextModel model = services.CreateTextModel(initialText, LanguageId);
        TestCodeEditor.WithTestCodeEditor(
            model,
            editor =>
            {
                if (tokens is not null)
                {
                    // Registered after the model is created, as in Monaco.
                    services.Add(TestLanguages.RegisterTokenizationSupport(services.TokenizationRegistry, services.LanguageIdCodec, tokens, LanguageId));
                }

                using var controller = new AutoIndentOnPaste(editor.ViewModel, () => EditorIndentationOptions.Default, services.LanguageConfigurationRegistry);
                test(editor, controller);
            },
            // An empty viewport: Monaco's test editor has no view, so no line is tokenized until it is forced (isCheapToTokenize).
            new TestCodeEditorOptions(OuterWidth: 0, OuterHeight: 0, CursorOptions: EditorCursorOptions.Default with { AutoIndent = EditorAutoIndentStrategy.Full }));
    }

    // Monaco: 'issue #119225: Do not add extra leading space when pasting JSDoc'
    [Fact]
    public void Trigger_PastedJsDoc_DoesNotAddLeadingSpace()
    {
        string pasteText = string.Join('\n', "/**", " * JSDoc", " */", "function a() {}");
        StandardTokenTypeData[][] tokens =
        [
            Line(StandardTokenType.Comment, 0, 3),
            Line(StandardTokenType.Comment, 0, 2, 8),
            [new(0, StandardTokenType.Comment), new(1, StandardTokenType.Comment), new(3, StandardTokenType.Other)],
            Line(StandardTokenType.Other, 0, 8, 9, 10, 11, 12, 13, 14, 15),
        ];

        Run(string.Empty, tokens, (editor, controller) =>
        {
            editor.ViewModel.Paste(pasteText, true, null, "keyboard");
            controller.Trigger(new Range(1, 1, 4, 16));
            Assert.Equal(pasteText, editor.Model.GetValue());
        });
    }

    // Monaco: 'issue #167299: Blank line removes indent'
    [Fact]
    public void Trigger_PastedTypeScriptWithBlankLines_KeepsTheIndent()
    {
        // no need for tokenization because there are no comments
        string pasteText = string.Join(
            '\n',
            string.Empty,
            "export type IncludeReference =",
            "\t| BaseReference",
            "\t| SelfReference",
            "\t| RelativeReference;",
            string.Empty,
            "export const enum IncludeReferenceKind {",
            "\tBase,",
            "\tSelf,",
            "\tRelativeReference,",
            "}");

        Run(string.Empty, null, (editor, controller) =>
        {
            editor.ViewModel.Paste(pasteText, true, null, "keyboard");
            controller.Trigger(new Range(1, 1, 11, 2));
            Assert.Equal(pasteText, editor.Model.GetValue());
        });
    }

    // Monaco: 'issue #29803: do not indent when pasting text with only one line'
    [Fact]
    public void Trigger_OneLinePaste_DoesNotIndent()
    {
        Run(string.Join('\n', "const linkHandler = new Class(a, b, c,", "    d)"), null, (editor, controller) =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(2, 6, 2, 6)]);
            editor.ViewModel.Paste(", null", true, null, "keyboard");
            controller.Trigger(new Range(2, 6, 2, 11));
            Assert.Equal(string.Join('\n', "const linkHandler = new Class(a, b, c,", "    d, null)"), editor.Model.GetValue());
        });
    }

    // Monaco: 'issue #29753: incorrect indentation after comment'
    [Fact]
    public void Trigger_PasteAfterComment_KeepsTheIndentation()
    {
        string initial = string.Join('\n', "class A {", "    /**", "     * used only for debug purposes.", "     */", "    private _codeInfo: KeyMapping[];", "}");
        Run(initial, null, (editor, controller) =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(5, 24, 5, 34)]);
            editor.ViewModel.Paste("IMacLinuxKeyMapping", true, null, "keyboard");
            controller.Trigger(new Range(5, 24, 5, 43));
            Assert.Equal(
                string.Join('\n', "class A {", "    /**", "     * used only for debug purposes.", "     */", "    private _codeInfo: IMacLinuxKeyMapping[];", "}"),
                editor.Model.GetValue());
        });
    }

    // Monaco: 'issue #29753: incorrect indentation of header comment'
    [Fact]
    public void Trigger_HeaderComment_IsNotIndented()
    {
        string text = string.Join('\n', "/*----------------", " *  Copyright (c) ", " *  Licensed under ...", " *-----------------*/");
        Run(string.Empty, null, (editor, controller) =>
        {
            editor.ViewModel.Paste(text, true, null, "keyboard");
            controller.Trigger(new Range(1, 1, 4, 22));
            Assert.Equal(text, editor.Model.GetValue());
        });
    }

    // Monaco: 'issue #209859: do not do change indentation when pasted inside of a string'
    [Fact]
    public void Trigger_PastedInsideAString_DoesNotChangeTheIndentation()
    {
        string initialText = string.Join('\n', "const foo = \"some text", "         which is strangely", "    indented\"");
        StandardTokenTypeData[][] tokens =
        [
            [new(0, StandardTokenType.Other), new(12, StandardTokenType.String)],
            Line(StandardTokenType.String, 0),
            Line(StandardTokenType.String, 0),
        ];

        Run(initialText, tokens, (editor, controller) =>
        {
            editor.ViewModel.SetSelections(null, [new Selection(2, 10, 2, 15)]);
            editor.ViewModel.Paste("which", true, null, "keyboard");
            controller.Trigger(new Range(2, 1, 2, 28));
            Assert.Equal(initialText, editor.Model.GetValue());
        });
    }

    private sealed class FakeClipboard : IClipboardService
    {
        public string? Text { get; set; }

        public void WriteText(string text) => Text = text;

        public Task<string?> ReadTextAsync() => Task.FromResult(Text);
    }

    private static EditorContext CreateContext(string text, bool autoIndentOnPaste, FakeClipboard clipboard, out EditorFeatureHost host)
    {
        var context = new EditorContext();
        context.RegisterLanguage(LanguageId);
        TestLanguages.RegisterLanguageConfiguration(context.Model.LanguageConfigurationRegistry, LanguageId);
        context.Model.Tokenization.SetLanguageId(LanguageId);
        context.SetText(text);
        var feature = new IndentationFeature { Options = new EditorIndentationOptions(AutoIndentOnPaste: autoIndentOnPaste) };
        host = new EditorFeatureHost(context, [() => feature]);
        ClipboardCommands.Register(context.Commands, context.ViewModel, clipboard);
        return context;
    }

    [Fact]
    public async Task DidPaste_OptionOff_DoesNotChangeTheText()
    {
        var clipboard = new FakeClipboard { Text = "a();\nb();" };
        using EditorContext context = CreateContext("function f() {\n\n}", false, clipboard, out EditorFeatureHost host);
        using var _ = host;
        context.Selection = Selection.Collapsed(new Position(2, 1));

        await ClipboardCommands.PasteAsync(context.ViewModel, clipboard);

        Assert.Equal("function f() {\na();\nb();\n}", context.Model.GetValue());
    }

    [Fact]
    public async Task DidPaste_OptionOn_ReindentsThePastedBlock()
    {
        var clipboard = new FakeClipboard { Text = "a();\nb();" };
        using EditorContext context = CreateContext("function f() {\n\n}", true, clipboard, out EditorFeatureHost host);
        using var _ = host;
        context.Selection = Selection.Collapsed(new Position(2, 1));

        await ClipboardCommands.PasteAsync(context.ViewModel, clipboard);

        Assert.Equal("function f() {\n    a();\n    b();\n}", context.Model.GetValue());
    }
}
