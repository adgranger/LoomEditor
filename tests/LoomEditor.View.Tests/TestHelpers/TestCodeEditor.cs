// Ported from microsoft/vscode: src/vs/editor/test/browser/testCodeEditor.ts (withTestCodeEditor, with testConfiguration.ts for phase 3)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.TestHelpers;

/// <summary>The options of <see cref="TestCodeEditor"/>.</summary>
internal sealed record TestCodeEditorOptions(
    EditorCursorOptions? CursorOptions = null,
    TextModelOptions? ModelOptions = null,
    int OuterWidth = 100,
    int OuterHeight = 100,
    bool HasTextFocus = true,
    EditorWrappingOptions? Wrapping = null,
    int EditorId = 0);

/// <summary>
/// A model and a view model, without any UI: what the ported cursor tests run against (Monaco: <c>withTestCodeEditor</c>).
/// <c>editor.executeEdits</c>, <c>getSelection</c> and <c>setSelections</c> are reached through <see cref="ViewModel"/>.
/// </summary>
/// <remarks>Threading: UI thread only (the frame scheduler never runs).</remarks>
internal sealed class TestCodeEditor : IDisposable
{
    // Monaco: TestConfiguration's mock FontInfo
    private static FontInfo CreateFontInfo() => new(1, "mockFont", "normal", 14, 19, 1.5, true, 10, 20, true, 10, 10, 10, 10);

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private TestCodeEditor(TextModel model, TestCodeEditorOptions options)
    {
        Model = model;
        FontInfo fontInfo = CreateFontInfo();
        ViewModel = new ViewModel(
            model,
            fontInfo,
            new EditorLayoutOptions { Wrapping = options.Wrapping ?? new() },
            new ViewLayoutOptions(fontInfo.LineHeight),
            _ => new NoopDisposable(),
            TimeProvider.System,
            options.EditorId);
        ViewModel.SetViewportSize(options.OuterWidth, options.OuterHeight);
        ViewModel.SetCursorOptions(options.CursorOptions ?? EditorCursorOptions.Default);
        if (options.HasTextFocus)
        {
            ViewModel.SetHasFocus(true);
        }
    }

    public TextModel Model { get; }

    public ViewModel ViewModel { get; }

    public void Dispose() => ViewModel.Dispose();

    public static void WithTestCodeEditor(string text, Action<TestCodeEditor> callback, TestCodeEditorOptions? options = null) =>
        WithTestCodeEditor(CreateTextModel(text, options?.ModelOptions), callback, options);

    public static void WithTestCodeEditor(string[] lines, Action<TestCodeEditor> callback, TestCodeEditorOptions? options = null) =>
        WithTestCodeEditor(string.Join('\n', lines), callback, options);

    public static void WithTestCodeEditor(TextModel model, Action<TestCodeEditor> callback, TestCodeEditorOptions? options = null)
    {
        using var editor = new TestCodeEditor(model, options ?? new TestCodeEditorOptions());
        callback(editor);
    }

    /// <summary>Monaco: <c>createTextModel</c> with <c>detectIndentation: false</c>.</summary>
    public static TextModel CreateTextModel(string text, TextModelOptions? options = null) => new(text, options: options);
}
