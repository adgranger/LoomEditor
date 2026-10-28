using System.Diagnostics;
using System.Text;
using LoomEditor.Core.Diff;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Threading;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Features.Scheduling;

namespace LoomEditor.Gallery;

/// <summary>The outcome of one check of <see cref="DiffSelfCheck"/>.</summary>
/// <param name="Name">The name of the check.</param>
/// <param name="Passed">Whether it passed.</param>
/// <param name="Detail">What was measured, or why it failed.</param>
internal sealed record DiffCheckResult(string Name, bool Passed, string Detail);

/// <summary>
/// Proves that the diff stack (algorithm, document diff provider, view model, alignment, accessible viewer model) works in the real app, the
/// Native AOT build included: <c>LoomEditor.Gallery.exe --diff-selfcheck=&lt;file&gt;</c> writes a PASS / FAIL report and exits 0 or 1.
/// </summary>
/// <remarks>Threading: UI thread only (the checks that use models and the dispatcher); no WinUI type: the caller gives the dispatcher and the scheduler.</remarks>
internal static class DiffSelfCheck
{
    /// <summary>The number of changes <see cref="DiffSamples"/> must give with <c>IgnoreTrimWhitespace</c> off.</summary>
    internal const int ExpectedSampleChanges = 9;

    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Runs every check; an exception or a timeout is a failed check, never a crash.</summary>
    /// <param name="dispatcher">Hands the results of the pool thread to the UI thread.</param>
    /// <param name="scheduler">The scheduler of the 200 ms debounce of the view model.</param>
    public static async Task<IReadOnlyList<DiffCheckResult>> RunAsync(IUiDispatcher dispatcher, IEditorScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(scheduler);

        List<DiffCheckResult> results = [];

        async Task Check(string name, Func<Task<string>> check)
        {
            try
            {
                results.Add(new DiffCheckResult(name, true, await check().WaitAsync(CheckTimeout)));
            }
            catch (TimeoutException)
            {
                results.Add(new DiffCheckResult(name, false, $"timeout after {CheckTimeout.TotalSeconds:0} s"));
            }
            catch (Exception e)
            {
                results.Add(new DiffCheckResult(name, false, e is CheckFailedException ? e.Message : $"{e.GetType().Name}: {e.Message}"));
            }
        }

        await Check("algorithm", () => Task.FromResult(Algorithm()));
        await Check("moves", () => Task.FromResult(Moves()));
        await Check("large", () => Task.FromResult(Large()));
        await Check("timeout", () => Task.FromResult(Timeout()));
        await Check("provider", Provider);
        await Check("provider-cancel", ProviderCancel);
        await Check("viewmodel", () => ViewModel(dispatcher, scheduler));
        await Check("alignment", () => Alignment(dispatcher, scheduler));
        await Check("accessible-model", () => AccessibleModel(dispatcher, scheduler));
        return results;
    }

    /// <summary>The report: one <c>PASS|FAIL  name — detail</c> line per check, then <c>SUMMARY: n passed, m failed</c>.</summary>
    public static string FormatReport(IReadOnlyList<DiffCheckResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        StringBuilder report = new();
        foreach (DiffCheckResult result in results)
        {
            report.Append(result.Passed ? "PASS" : "FAIL").Append("  ").Append(result.Name).Append(" — ").Append(result.Detail).Append('\n');
        }

        int passed = results.Count(r => r.Passed);
        report.Append(System.Globalization.CultureInfo.InvariantCulture, $"SUMMARY: {passed} passed, {results.Count - passed} failed\n");
        return report.ToString();
    }

    /// <summary>Runs the checks, writes the report (UTF-8) and returns the number of failures.</summary>
    public static async Task<int> RunToFileAsync(string reportFile, IUiDispatcher dispatcher, IEditorScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(reportFile);

        IReadOnlyList<DiffCheckResult> results = await RunAsync(dispatcher, scheduler);
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

    private static LinesDiff Compute(string[] original, string[] modified, bool ignoreTrimWhitespace, int maxMs, bool computeMoves) =>
        new DefaultLinesDiffComputer().ComputeDiff(original, modified, new LinesDiffComputerOptions(ignoreTrimWhitespace, maxMs, computeMoves));

    // Monaco: assertDiffCorrectness. Applying the inner changes to the original text must give the modified text.
    private static string Algorithm()
    {
        string[] original = DiffSamples.Original.Split('\n');
        string[] modified = DiffSamples.Modified.Split('\n');
        LinesDiff diff = Compute(original, modified, false, 5000, false);

        Require(diff.Changes.Count == ExpectedSampleChanges, $"{diff.Changes.Count} change(s), expected {ExpectedSampleChanges}");
        string rebuilt = ApplyInnerChanges(diff, original, modified);
        Require(rebuilt == DiffSamples.Modified, "applying the inner changes to the original does not give the modified text");
        return $"{diff.Changes.Count} changes, the inner changes rebuild the modified text";
    }

    private static string ApplyInnerChanges(LinesDiff diff, string[] original, string[] modified)
    {
        string originalText = string.Join('\n', original);
        string modifiedText = string.Join('\n', modified);
        StringBuilder sb = new();
        int last = 0;
        foreach (RangeMapping m in diff.Changes.SelectMany(c => c.InnerChanges ?? []))
        {
            int start = Offset(original, m.OriginalRange.StartLineNumber, m.OriginalRange.StartColumn);
            int end = Offset(original, m.OriginalRange.EndLineNumber, m.OriginalRange.EndColumn);
            int modifiedStart = Offset(modified, m.ModifiedRange.StartLineNumber, m.ModifiedRange.StartColumn);
            int modifiedEnd = Offset(modified, m.ModifiedRange.EndLineNumber, m.ModifiedRange.EndColumn);
            sb.Append(originalText, last, start - last).Append(modifiedText, modifiedStart, modifiedEnd - modifiedStart);
            last = end;
        }

        return sb.Append(originalText, last, originalText.Length - last).ToString();
    }

    private static int Offset(string[] lines, int line, int column)
    {
        int offset = 0;
        for (int i = 0; i < line - 1; i++)
        {
            offset += lines[i].Length + 1;
        }

        return offset + column - 1;
    }

    private static string Moves()
    {
        LinesDiff diff = Compute(DiffSamples.Original.Split('\n'), DiffSamples.Modified.Split('\n'), true, 5000, true);

        Require(diff.Moves is not null, "no list of moves");
        return $"{diff.Moves!.Count} move(s)";
    }

    private static string Large()
    {
        (string original, string modified) = DiffSamples.CreateLargePair(3000);
        Stopwatch watch = Stopwatch.StartNew();
        LinesDiff diff = Compute(original.Split('\n'), modified.Split('\n'), true, 5000, false);
        watch.Stop();

        Require(watch.Elapsed < TimeSpan.FromSeconds(5), $"{watch.ElapsedMilliseconds} ms");
        Require(diff.Changes.Count > 0, "no change found");
        return $"{diff.Changes.Count} changes in {watch.ElapsedMilliseconds} ms";
    }

    // Both outcomes are fine: the computation stops early, or it was fast enough. Only an exception fails.
    private static string Timeout()
    {
        (string original, string modified) = DiffSamples.CreateLargePair(3000);
        LinesDiff diff = Compute(original.Split('\n'), modified.Split('\n'), true, 1, false);

        return $"HitTimeout = {diff.HitTimeout}, {diff.Changes.Count} changes";
    }

    private static async Task<string> Provider()
    {
        var original = new TextModel(DiffSamples.Original);
        var modified = new TextModel(DiffSamples.Modified);
        var options = new LinesDiffComputerOptions(true, 5000, false);

        DocumentDiff viaProvider = await new DocumentDiffProvider().ComputeDiffAsync(original, modified, options, CancellationToken.None);
        LinesDiff direct = Compute(DiffSamples.Original.Split('\n'), DiffSamples.Modified.Split('\n'), true, 5000, false);

        Require(viaProvider.Changes.Count == direct.Changes.Count, $"{viaProvider.Changes.Count} change(s) through the provider, {direct.Changes.Count} directly");
        for (int i = 0; i < direct.Changes.Count; i++)
        {
            Require(
                viaProvider.Changes[i].Original == direct.Changes[i].Original && viaProvider.Changes[i].Modified == direct.Changes[i].Modified,
                $"change {i} differs");
        }

        return $"{viaProvider.Changes.Count} changes, the same as the direct computation";
    }

    private static async Task<string> ProviderCancel()
    {
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();
        var original = new TextModel(DiffSamples.Original);
        var modified = new TextModel(DiffSamples.Modified);

        try
        {
            DocumentDiff result = await new DocumentDiffProvider().ComputeDiffAsync(original, modified, new LinesDiffComputerOptions(true, 5000, false), cts.Token);
            Require(result.QuitEarly, "a cancelled computation returned a complete result");
            return "quit early";
        }
        catch (OperationCanceledException)
        {
            return "cancelled";
        }
    }

    private static async Task<string> ViewModel(IUiDispatcher dispatcher, IEditorScheduler scheduler)
    {
        var original = new TextModel(DiffSamples.Original);
        var modified = new TextModel(DiffSamples.Modified);
        var options = new DiffEditorOptions { HideUnchangedRegions = new HideUnchangedRegionsOptions(true, 3, 3, 20) };
        using var viewModel = new DiffEditorViewModel(original, modified, options, new DocumentDiffProvider(), dispatcher, scheduler);

        await viewModel.WaitForDiffAsync();
        Require(viewModel.Diff is not null, "no diff after WaitForDiffAsync");
        DiffState first = viewModel.Diff!;
        Require(viewModel.UnchangedRegions.Count > 0, "no unchanged region");

        modified.ApplyEdits(new[] { new TextEdit(new Range(1, 1, 1, 1), "// edited\n") });
        await viewModel.WaitForDiffAsync();
        Require(viewModel.IsDiffUpToDate && !ReferenceEquals(first, viewModel.Diff), "no new diff after an edit");
        return $"{first.Mappings.Count} then {viewModel.Diff!.Mappings.Count} changes, {viewModel.UnchangedRegions.Count} unchanged region(s)";
    }

    private static async Task<DiffEditorViewModel> StartViewModel(IUiDispatcher dispatcher, IEditorScheduler scheduler, TextModel original, TextModel modified)
    {
        var viewModel = new DiffEditorViewModel(original, modified, new DiffEditorOptions(), new DocumentDiffProvider(), dispatcher, scheduler);
        await viewModel.WaitForDiffAsync();
        return viewModel;
    }

    private static async Task<string> Alignment(IUiDispatcher dispatcher, IEditorScheduler scheduler)
    {
        var original = new TextModel(DiffSamples.Original);
        var modified = new TextModel(DiffSamples.Modified);
        using DiffEditorViewModel viewModel = await StartViewModel(dispatcher, scheduler, original, modified);

        IReadOnlyList<LineRangeAlignment> blocks = LineRangeAlignments.Compute(
            viewModel.Diff!.Mappings, [], [], 20, 20, line => original.GetLineMaxColumn(line), original.LineCount, innerHunkAlignment: false);

        // Without extra heights the blocks are the changed areas (the unchanged lines are aligned by construction): in order, as high as their lines, and
        // together as long as the changes on both sides.
        Require(blocks.Count > 0, "no block");
        int previousOriginalEnd = 1;
        int previousModifiedEnd = 1;
        foreach (LineRangeAlignment block in blocks)
        {
            Require(
                block.OriginalRange.StartLineNumber >= previousOriginalEnd && block.ModifiedRange.StartLineNumber >= previousModifiedEnd,
                $"the block {block.OriginalRange}/{block.ModifiedRange} overlaps the previous one");
            Require(
                block.OriginalHeightInPx == block.OriginalRange.Length * 20 && block.ModifiedHeightInPx == block.ModifiedRange.Length * 20,
                $"the heights of the block {block.OriginalRange}/{block.ModifiedRange} are not its line counts times the line height");
            previousOriginalEnd = block.OriginalRange.EndLineNumberExclusive;
            previousModifiedEnd = block.ModifiedRange.EndLineNumberExclusive;
        }

        int changedOriginal = viewModel.Diff.Mappings.Sum(m => m.LineRangeMapping.Original.Length);
        int changedModified = viewModel.Diff.Mappings.Sum(m => m.LineRangeMapping.Modified.Length);
        Require(blocks.Sum(b => b.OriginalRange.Length) == changedOriginal, "the blocks do not cover the changed lines of the original");
        Require(blocks.Sum(b => b.ModifiedRange.Length) == changedModified, "the blocks do not cover the changed lines of the modified text");        return $"{blocks.Count} blocks cover {changedOriginal} and {changedModified} changed lines";
    }

    private static async Task<string> AccessibleModel(IUiDispatcher dispatcher, IEditorScheduler scheduler)
    {
        var original = new TextModel(DiffSamples.Original);
        var modified = new TextModel(DiffSamples.Modified);
        using DiffEditorViewModel viewModel = await StartViewModel(dispatcher, scheduler, original, modified);

        var model = new AccessibleDiffViewerModel(new SampleHost(original, modified), canClose: true);
        model.SetDiffs(viewModel.Diff!.Mappings.Select(m => m.LineRangeMapping).ToList());

        Require(model.Groups.Count > 0, "no group");
        ViewElementGroup group = model.Groups[0];
        string Lines(int n) => n == 0 ? "no lines changed" : n == 1 ? "1 line changed" : $"{n} lines changed";
        string expected =
            $"Difference 1 of {model.Groups.Count}: original line {group.Range.Original.StartLineNumber}, {Lines(group.Range.Original.Length)}, " +
            $"modified line {group.Range.Modified.StartLineNumber}, {Lines(group.Range.Modified.Length)}";
        string actual = LoomEditor.View.Accessibility.AccessibilityMessages.DiffHeaderLabel(
            1, model.Groups.Count, group.Range.Original.StartLineNumber, group.Range.Original.Length, group.Range.Modified.StartLineNumber, group.Range.Modified.Length);

        Require(actual == expected, $"'{actual}' instead of '{expected}'");
        return $"{model.Groups.Count} group(s); {actual}";
    }

    private sealed class CheckFailedException(string message) : Exception(message);

    // The accessible viewer model only reads the models here: the reveal and focus members are never called.
    private sealed class SampleHost(TextModel original, TextModel modified) : IAccessibleDiffViewerHost
    {
        public int OriginalLineCount => original.LineCount;

        public int ModifiedLineCount => modified.LineCount;

        public int? ModifiedCursorLine => null;

        public string GetOriginalLine(int lineNumber) => original.GetLineContent(lineNumber);

        public string GetModifiedLine(int lineNumber) => modified.GetLineContent(lineNumber);

        public void OriginalReveal(Range range)
        {
        }

        public void ModifiedReveal(Range? range)
        {
        }

        public void ModifiedSetSelection(Range range)
        {
        }

        public void ModifiedFocus()
        {
        }

        public void SetViewerVisible(bool visible)
        {
        }
    }
}
