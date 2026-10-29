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

The demo is in the Gallery (**Diff editor window**, `samples/LoomEditor.Gallery/DiffDemoWindow.xaml`); the manual plan is
[testing/phase-9-manual-tests.md](../testing/phase-9-manual-tests.md).
