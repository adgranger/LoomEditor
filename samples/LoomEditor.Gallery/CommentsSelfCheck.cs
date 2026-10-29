using System.Globalization;
using System.Text;
using LoomEditor.Core.Comments;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Comments;
using LoomEditor.View.Features;
using LoomEditor.View.Layout;
using LoomEditor.View.Markdown;
using TextRange = LoomEditor.Core.Text.Range;

namespace LoomEditor.Gallery;

/// <summary>The outcome of one check of <see cref="CommentsSelfCheck"/>.</summary>
/// <param name="Name">The name of the check.</param>
/// <param name="Passed">Whether it passed.</param>
/// <param name="Detail">What was measured, or why it failed.</param>
internal sealed record CommentsCheckResult(string Name, bool Passed, string Detail);

/// <summary>
/// Proves that the phase 11 logic (comment threads, Markdown extensions, toolbar, suggestions, view model, manager, controller, relative time)
/// works in the real app, the Native AOT build included: <c>LoomEditor.Gallery.exe --comments-selfcheck=&lt;file&gt;</c> writes a PASS / FAIL
/// report and exits 0 or 1. The WinUI widget itself (layer, height, focus, composer, accessibility) is not covered.
/// </summary>
/// <remarks>Threading: UI thread only (the checks create editor contexts); no WinUI type.</remarks>
internal static class CommentsSelfCheck
{
    private static readonly CommentAuthor Author = new("Ada");

    /// <summary>The names of the checks, in run order.</summary>
    public static IReadOnlyList<string> Names { get; } =
        (string[])["thread-model", "markdown-extended", "toolbar-inline", "toolbar-block", "suggestion-extract", "suggestion-edit", "suggestion-diff", "view-model", "relative-time", "thread-manager", "controller"];

    /// <summary>Runs every check; an exception is a failed check, never a crash.</summary>
    public static IReadOnlyList<CommentsCheckResult> Run()
    {
        List<CommentsCheckResult> results = [];

        void Check(string name, Func<string> check)
        {
            try
            {
                results.Add(new CommentsCheckResult(name, true, check()));
            }
            catch (Exception e)
            {
                results.Add(new CommentsCheckResult(name, false, e is CheckFailedException ? e.Message : $"{e.GetType().Name}: {e.Message}"));
            }
        }

        Check("thread-model", ThreadModel);
        Check("markdown-extended", MarkdownExtended);
        Check("toolbar-inline", ToolbarInline);
        Check("toolbar-block", ToolbarBlock);
        Check("suggestion-extract", SuggestionExtract);
        Check("suggestion-edit", SuggestionEditCheck);
        Check("suggestion-diff", SuggestionDiffCheck);
        Check("view-model", ViewModel);
        Check("relative-time", RelativeTimeCheck);
        Check("thread-manager", ThreadManager);
        Check("controller", Controller);
        return results;
    }

    /// <summary>The report: <c>LoomEditor comments self-check</c>, one <c>PASS|FAIL name — detail</c> line per check, then <c>n of m checks passed</c>.</summary>
    public static string FormatReport(IReadOnlyList<CommentsCheckResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        StringBuilder report = new("LoomEditor comments self-check\n");
        foreach (CommentsCheckResult result in results)
        {
            report.Append(result.Passed ? "PASS" : "FAIL").Append(' ').Append(result.Name).Append(" — ").Append(result.Detail).Append('\n');
        }

        report.Append(CultureInfo.InvariantCulture, $"{results.Count(r => r.Passed)} of {results.Count} checks passed\n");
        return report.ToString();
    }

    /// <summary>Runs the checks, writes the report (UTF-8) and returns the number of failures.</summary>
    public static int RunToFile(string reportFile)
    {
        ArgumentNullException.ThrowIfNull(reportFile);

        IReadOnlyList<CommentsCheckResult> results = Run();
        File.WriteAllText(reportFile, FormatReport(results), new UTF8Encoding(false));
        return results.Count(r => !r.Passed);
    }

    private static void Require(bool condition, string failure)
    {
        if (!condition)
        {
            throw new CheckFailedException(failure);
        }
    }

    private static Comment Suggest(string suggestion) => Comment.Create(Author, "```suggestion\n" + suggestion + "\n```");

    private static string Escape(string text) => text.Replace("\r", "\\r").Replace("\n", "\\n");

    private static string ThreadModel()
    {
        var thread = new CommentThread("t1", 3);
        List<CommentThreadChangeKind> kinds = [];
        thread.Changed += (_, e) => kinds.Add(e.Kind);

        Comment first = Comment.Create(Author, "one");
        thread.AddComment(first);
        Require(thread.ReplaceComment(first with { Body = "uno" }) && thread.Comments[0].Body == "uno", "replace failed");
        bool duplicate = false;
        try
        {
            thread.AddComment(first);
        }
        catch (ArgumentException)
        {
            duplicate = true;
        }

        Require(duplicate, "a duplicate id was accepted");
        Require(thread.RemoveComment(first.Id) && thread.Comments.Count == 0, "remove failed");
        Require(
            string.Join(',', kinds) == "CommentAdded,CommentReplaced,CommentRemoved",
            $"events [{string.Join(',', kinds)}]");
        return "add, replace, remove raise their events; a duplicate id is rejected";
    }

    private static string Describe(MarkdownDocument document) => string.Join(',', document.Blocks.Select(b => b.GetType().Name));

    private static string MarkdownExtended()
    {
        MarkdownDocument quote = MarkdownParser.Parse("> q", MarkdownParserOptions.Extended);
        Require(quote.Blocks.Count == 1 && quote.Blocks[0] is MarkdownQuote, $"'> q' gave {Describe(quote)} under Extended");

        MarkdownDocument rich = MarkdownParser.Parse("~~s~~ <u>u</u>", MarkdownParserOptions.Extended);
        var paragraph = rich.Blocks.SingleOrDefault() as MarkdownParagraph ?? throw new CheckFailedException($"rich text gave {Describe(rich)}");
        Require(paragraph.Inlines.Any(i => i.Strikethrough && i.Text == "s"), "no strikethrough 's'");
        Require(paragraph.Inlines.Any(i => i.Underline && i.Text == "u"), "no underline 'u'");

        MarkdownDocument plainQuote = MarkdownParser.Parse("> q", MarkdownParserOptions.Default);
        MarkdownDocument plainRich = MarkdownParser.Parse("~~s~~ <u>u</u>", MarkdownParserOptions.Default);
        Require(plainQuote.Blocks.All(b => b is MarkdownParagraph), $"'> q' gave {Describe(plainQuote)} without Extended");
        Require(
            plainRich.Blocks.All(b => b is MarkdownParagraph p && !p.Inlines.Any(i => i.Strikethrough || i.Underline)),
            "extended inlines parsed without Extended");
        return "quote, strikethrough and underline under Extended; plain paragraphs without it";
    }

    private static ToolbarEdit Toolbar(MarkdownToolbarAction action, string text, int start, int end, string? seed = null) =>
        MarkdownToolbar.Apply(action, text, new ToolbarSelection(start, end), seed);

    private static string ToolbarInline()
    {
        ToolbarEdit bold = Toolbar(MarkdownToolbarAction.Bold, "x a y", 2, 3);
        Require(bold.Text == "x **a** y" && bold.Selection == new ToolbarSelection(2, 7), $"bold gave '{bold.Text}' {bold.Selection}");

        ToolbarEdit unwrap = Toolbar(MarkdownToolbarAction.Bold, bold.Text, bold.Selection.Start, bold.Selection.End);
        Require(unwrap.Text == "x a y", $"unwrap gave '{unwrap.Text}'");

        ToolbarEdit link = Toolbar(MarkdownToolbarAction.Link, "https://x.io", 0, 12);
        Require(link.Text == "[](https://x.io)" && link.Selection == new ToolbarSelection(1, 1), $"link gave '{link.Text}' {link.Selection}");
        return "bold wraps and unwraps; a URL selection becomes the link target";
    }

    private static string ToolbarBlock()
    {
        ToolbarEdit numbered = Toolbar(MarkdownToolbarAction.NumberedList, "a\nb\nc", 0, 5);
        Require(numbered.Text == "1. a\n2. b\n3. c", $"numbered list gave '{Escape(numbered.Text)}'");

        ToolbarEdit code = Toolbar(MarkdownToolbarAction.CodeBlock, "a\nb", 0, 3);
        Require(code.Text == "```\na\nb\n```", $"code block gave '{Escape(code.Text)}'");

        ToolbarEdit suggestion = Toolbar(MarkdownToolbarAction.Suggestion, "", 0, 0, "int x;");
        Require(
            suggestion.Text == "```suggestion\nint x;\n```" && suggestion.Selection == new ToolbarSelection(14, 20),
            $"suggestion gave '{Escape(suggestion.Text)}' {suggestion.Selection}");
        return "numbered list on three lines, code block and seeded suggestion";
    }

    private static string SuggestionExtract()
    {
        IReadOnlyList<SuggestionBlock> blocks = SuggestionExtractor.Extract("a\n```suggestion\nx\ny\n```\n```csharp\nint z;\n```\n```suggestion\n```\n");

        Require(blocks.Count == 2, $"{blocks.Count} blocks, expected 2");
        Require(blocks[0].Text == "x\ny" && !blocks[0].IsDeletion, $"first block '{Escape(blocks[0].Text)}'");
        Require(blocks[1].IsDeletion, "the empty block is not a deletion");
        return "two suggestion blocks (the C# fence ignored); the empty one is a deletion";
    }

    private static string Applied(string text, int line, string suggestion)
    {
        var model = new TextModel(text);
        TextEdit edit = SuggestionEdit.Create(model, line, new SuggestionBlock(0, suggestion)) ?? throw new CheckFailedException("no edit");
        model.ApplyEdits(new[] { edit });
        return model.GetValue();
    }

    private static string SuggestionEditCheck()
    {
        string single = Applied("a\r\nb\r\nc", 2, "X");
        Require(single == "a\r\nX\r\nc", $"single line gave '{Escape(single)}'");

        string multi = Applied("a\r\nb\r\nc", 2, "X\nY");
        Require(multi == "a\r\nX\r\nY\r\nc", $"multi line gave '{Escape(multi)}'");

        string deletion = Applied("a\r\nb\r\nc", 3, "");
        Require(deletion == "a\r\nb", $"deleting the last line gave '{Escape(deletion)}'");
        return "single line, multi line and last-line deletion keep CRLF";
    }

    private static string SuggestionDiffCheck()
    {
        SuggestionDiffResult replace = SuggestionDiff.Compute("old", new SuggestionBlock(0, "new"));
        Require(string.Concat(replace.Rows.Select(r => r.Marker)) == "-+" && !replace.IsNoOp && !replace.IsDeletion, "replacement rows");

        SuggestionDiffResult deletion = SuggestionDiff.Compute("old", new SuggestionBlock(0, ""));
        Require(deletion.IsDeletion && deletion.Rows.Count == 1 && deletion.Rows[0].Marker == '-', "deletion rows");

        SuggestionDiffResult noOp = SuggestionDiff.Compute("old", new SuggestionBlock(0, "old"));
        Require(noOp.IsNoOp && noOp.Describe() == CommentStrings.NoChanges + ".", $"no-op described '{noOp.Describe()}'");
        Require(replace.Describe().Length > 0 && deletion.Describe().Length > 0, "empty description");
        return "replacement is -/+, deletion is -, no-op is one context row; all described";
    }

    private static string ViewModel()
    {
        var thread = new CommentThread("t", 1);
        using var viewModel = new CommentThreadViewModel(thread, () => "seed");
        List<string> submitted = [];
        viewModel.SubmitRequested += (_, text) => submitted.Add(text);

        viewModel.StartComposing();
        viewModel.SetDraft("  \n");
        Require(!viewModel.CanSubmit && viewModel.Submit() is null, "a blank draft can be submitted");
        Require(viewModel.TryEscape() && !viewModel.IsComposing, "Escape on a blank draft did not close the composer");

        viewModel.StartComposing();
        viewModel.SetDraft("hello\r\n");
        Require(viewModel.Draft == "hello\n" && !viewModel.TryEscape(), "Escape closed a composer with a draft");
        Require(viewModel.Submit() == "hello" && viewModel.Draft == "hello\n" && submitted.Count == 1, "submit did not keep the draft");

        thread.AddComment(Comment.Create(Author, "hello"));
        Require(viewModel.Draft.Length == 0 && !viewModel.IsComposing, "the added comment did not clear the draft");
        return "blank draft cannot submit, submit keeps the draft until the comment is added, Escape only closes a blank draft";
    }

    private static string RelativeTimeCheck()
    {
        var now = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        string[] actual =
        [
            RelativeTime.Format(now.AddSeconds(-10), now),
            RelativeTime.Format(now.AddMinutes(-5), now),
            RelativeTime.Format(now.AddHours(-3), now),
            RelativeTime.Format(now.AddHours(-30), now),
            RelativeTime.Format(now.AddDays(-40), now),
        ];
        string[] expected = ["just now", "5 minutes ago", "3 hours ago", "yesterday", "2026-05-06"];

        Require(actual.SequenceEqual(expected), $"[{string.Join(" | ", actual)}], expected [{string.Join(" | ", expected)}]");
        return string.Join(", ", actual);
    }

    private static string ThreadManager()
    {
        using var context = new EditorContext();
        context.SetText("l1\nl2\nl3");
        using var manager = new CommentThreadManager(context.Model, context.ViewModel.ViewZones, context.Id);
        CommentThread thread = manager.Add(2);

        context.Model.ApplyEdits(new[] { new TextEdit(new TextRange(1, 1, 1, 1), "new\n") });
        Require(thread.LineNumber == 3, $"the thread is on line {thread.LineNumber} after an insertion above, expected 3");

        ViewZone zone = context.ViewModel.ViewZones.GetZone(manager.GetZoneId(thread)!) ?? throw new CheckFailedException("no zone");
        Require(zone.Kind == ViewZoneKind.Widget && zone.AfterLineNumber == 3, $"zone {zone.Kind} after line {zone.AfterLineNumber}");

        manager.SetHeight(thread, 200);
        Require(manager.GetHeight(thread) == 200 && zone.HeightInPx == 200, "SetHeight did not change the zone");
        return "the thread follows an inserted line; its Widget zone follows and resizes";
    }

    private static string Controller()
    {
        using var context = new EditorContext();
        context.SetText("one\ntwo\nthree");
        using var controller = new CommentThreadsController(context);
        CommentThread thread = controller.Add(2, null, false);
        Comment comment = Suggest("a\nb");
        thread.AddComment(comment);

        ApplySuggestionResult result = controller.ApplySuggestion(thread, comment.Id, 0, "two");
        Require(result == ApplySuggestionResult.Applied, $"ApplySuggestion gave {result}");
        Require(context.Model.GetValue() == "one\na\nb\nthree", $"text '{Escape(context.Model.GetValue())}' after apply");

        context.Model.Undo();
        Require(context.Model.GetValue() == "one\ntwo\nthree", $"text '{Escape(context.Model.GetValue())}' after one undo");
        return "ApplySuggestion returns Applied; one undo restores the text";
    }

    private sealed class CheckFailedException(string message) : Exception(message);
}
