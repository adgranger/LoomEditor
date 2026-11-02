# Use the diff editor

`LoomDiffEditor` shows two texts side by side (or inline) with the changes highlighted. It owns two `LoomEditor` controls, available as
`Original` and `Modified`. The design is in [ADR 0012](../adr/0012-diff-editor.md).

## XAML and code

```xml
<Window xmlns:loom="using:LoomEditor.WinUI.Controls">
    <loom:LoomDiffEditor x:Name="Diff" LanguageId="csharp" RenderSideBySide="True" HideUnchangedRegions="True" />
</Window>
```

```csharp
Diff.Theme = EditorTheme.Dark;
Diff.OriginalText = File.ReadAllText("Old.cs");
Diff.ModifiedText = File.ReadAllText("New.cs");

Diff.DiffChanged += (_, _) => status.Text = $"{Diff.ChangeCount} change(s), up to date: {Diff.IsDiffUpToDate}";
await Diff.WaitForDiffAsync();      // completes when the first diff (or the next one after an edit) is published
await Diff.RevealFirstChangeAsync();
```

`OriginalText` and `ModifiedText` follow the edits made in the editors. Without `Language` the text is plain; `LanguageId` and `Theme`
apply to both sides.

## Options

| Property | Default | What it does |
|---|---|---|
| `RenderSideBySide` | `true` | side by side, or inline (deleted lines drawn in the modified editor) |
| `UseInlineViewWhenSpaceIsLimited` | `true` | switch to inline below 900 DIPs of width |
| `IgnoreTrimWhitespace` | `true` | whitespace at the start and end of a line is not a change |
| `HideUnchangedRegions` (+ `...ContextLineCount`, `...MinimumLineCount`, `...RevealLineCount`) | `false` (3, 3, 20) | collapse long unchanged blocks behind a bar |
| `RenderIndicators` | `true` | `+` / `−` signs in the margin |
| `OriginalEditable`, `IsReadOnly` | `false` | the original side is read-only by default; `IsReadOnly` is the modified side |
| `RenderOverviewRuler` | `true` | the strip with the marks and the viewport slider |
| `EnableSplitViewResizing` | `true` | drag the sash; double click resets it |
| `MaxComputationTimeMs` | `5000` | the diff stops early after this time |

## Navigation and the accessible viewer

- `Alt+F5` / `Shift+Alt+F5`, or `GoToNextChange()` / `GoToPreviousChange()`: move the cursor of the modified editor to the next / previous
  change, wrapping around.
- `CollapseAllUnchangedRegions()` / `ShowAllUnchangedRegions()` with `HideUnchangedRegions` on.
- `F7` / `Shift+F7`, or `OpenAccessibleDiffViewer()`: the accessible diff viewer, a list that covers both editors and shows one group of
  changes row by row (arrows, `Enter` / `Space` reveal the line, `Esc` closes). `IsAccessibleDiffViewerOpen` tells whether it is open.

## Comment threads

The single-editor API, the events and a suggestion example are in [use-comment-threads.md](use-comment-threads.md).

`AddCommentThread(DiffSide.Original | DiffSide.Modified, line, title, startComposing)` puts a thread under a line of either side; in the
side-by-side view the other side gets the same blank height, so the lines stay level. `CommentThreads` lists both sides, `GetSide(thread)`
tells which one a thread is on, `RemoveCommentThread` works for both. `CommentSubmitted`, `CommentCancelled` and `SuggestionApplied` come from
either side with the same arguments as `LoomEditor` (call `GetSide(e.Thread)`); `AddCommentRequested` carries a `DiffAddCommentRequestedEventArgs`
with `Side` and `LineNumber`. Threads added through `Original` / `Modified` directly are included. The gutter "+" button is per editor: set
`CommentGutterButtonEnabled` on `Original` and `Modified`. In the inline view the original side's threads are not shown (they come back, with
their drafts, in side-by-side). A suggestion cannot be applied on the original side unless `OriginalEditable` is on. When `OriginalText` or
`ModifiedText` replaces a text, its threads stay, clamped to the new line count.

## Counting the changes

`ChangeCount` is the number of blocks of changed lines of the last diff; `IsDiffUpToDate` is false from an edit until its new diff is
published (the old highlights stay meanwhile). Wait for `DiffChanged` or `WaitForDiffAsync()` before reading the count of a new text.

## Line and block actions (staging)

`LoomDiffEditor` knows nothing about git: it shows the buttons and checkboxes, raises events with line numbers, and changes no text. A git client (VIGO) turns the
lines into hunks and patches. Everything below works in the side-by-side view only (no lane, checkbox or button in the inline view); `Changes` and `SelectedLines`
work in both.

```csharp
Diff.BlockActionsVisibility = DiffBlockActionsVisibility.OnHover;   // Off (default), OnHover, Always
Diff.PrimaryBlockActionGlyph = "\uE710";                            // a Segoe Fluent Icons character; null hides the action
Diff.PrimaryBlockActionToolTip = "Stage block";
Diff.SecondaryBlockActionGlyph = "\uE7A7";
Diff.SecondaryBlockActionToolTip = "Discard block";
Diff.LineSelectionEnabled = true;                                   // a checkbox on every changed line
Diff.LineActionGlyph = "\uE710";                                    // a button on the hovered changed line
Diff.LineActionToolTip = "Stage line";                              // LineActionKey (default S) does the same on a read-only side

// A block is one hunk. Git counts the start of an empty side as "the line after which", LoomEditor as "the line before which".
static int GitStart(int start, int count) => count == 0 ? start - 1 : start;

Diff.BlockActionInvoked += (_, e) =>
{
    DiffChangeBlock b = e.Block;
    string header = $"@@ -{GitStart(b.OriginalStartLineNumber, b.OriginalLineCount)},{b.OriginalLineCount} " +
                    $"+{GitStart(b.ModifiedStartLineNumber, b.ModifiedLineCount)},{b.ModifiedLineCount} @@";
    if (e.Action == DiffBlockAction.Primary) { repo.ApplyToIndex(BuildHunk(header, b)); } else { repo.Discard(BuildHunk(header, b)); }
};

// Some lines: a partial patch that keeps only the selected deleted (Original) and added (Modified) lines of each block.
Diff.LineActionInvoked += (_, e) => repo.ApplyToIndex(BuildPartialPatch(Diff.Changes, e.Lines));
Diff.SelectionChanged += (_, _) => stageSelected.IsEnabled = Diff.SelectedLines.Count > 0;
Diff.DiffChanged += (_, _) => blocks.Text = $"{Diff.Changes.Count} block(s)";
```

Every property above is a dependency property (a `…Property` field each), so it can be set in XAML.

- **`Changes`**: the blocks of the last published diff, in order (empty before the first one; `DiffChanged` is the change notification). Each `DiffChangeBlock`
  has `OriginalStartLineNumber` / `OriginalLineCount`, `ModifiedStartLineNumber` / `ModifiedLineCount` (1-based model lines of that side) and `InnerChanges`
  (`DiffInnerChange(OriginalRange, ModifiedRange)`, the character changes; may be empty). With `IgnoreTrimWhitespace` on (the default), a whitespace-only change is
  not a block.
- **Empty sides**: a count of 0 is a pure insertion (original) or a pure deletion (modified). Its start line is the line **before which** the other side's lines sit,
  and can be the side's line count + 1. A git hunk header wants `start - 1` there (`GitStart` above): `@@ -4,0 +5,2 @@` is the block `5,0 -> 5,2`.
- **Line selection**: `SelectedLines` is a list of `DiffLine(DiffSide Side, int LineNumber)`, original side first, each side ascending. A click toggles one line,
  Ctrl+click toggles the line and its partner (same index in the same block, on the other side), Shift+click extends on the same side. `SetSelection(lines)` ignores
  lines that are not changed lines; `ClearSelection()`; turning `LineSelectionEnabled` off clears it. A new diff keeps the selection (minus lines that are no longer
  changed) when neither text was edited since the previous diff, and clears it otherwise.
- **Line action**: `LineActionInvoked.Lines` is the selection when the clicked line is selected (or, for the key, when there is a selection), else the one line of the
  button or the caret; never empty.
- **Block buttons** sit at the first line of each visible block in the modified editor (the original editor for a pure deletion); `BlockActionInvoked` carries the
  `Block` and the `Action` (`Primary` / `Secondary`). They are not tab stops: a keyboard user reaches the blocks with `GoToNextChange` and the caret, or with the line
  action key.

The public API of this feature (phase 14) is listed in [tasks/phase-14/README.md](../tasks/phase-14/README.md#public-api-of-the-phase-the-names-are-final); the
manual plan is [testing/phase-14-manual-tests.md](../testing/phase-14-manual-tests.md).

The demo is in the Gallery (**Diff editor window**, `samples/LoomEditor.Gallery/DiffDemoWindow.xaml`); the manual plan is
[testing/phase-9-manual-tests.md](../testing/phase-9-manual-tests.md).
