# P11-04 — Markdown toolbar (2/2): heading, lists, quote, code block and suggestion

- **Status:** done
- **Depends on:** P11-03
- **Size:** M

## Goal

`MarkdownToolbar.Apply` also handles `Heading`, `BulletList`, `NumberedList`, `Quote`, `CodeBlock` and `Suggestion`. The toolbar is complete as pure logic.

## Sources

Sources: none (specified in this card; ADR 0014). Read first: `MarkdownToolbar.cs`, `MarkdownToolbar.Inline.cs` and their tests (P11-03).

## Files to create or change

- `src/LoomEditor.View/Comments/MarkdownToolbar.cs` — change: the block branch dispatches to the new file
- `src/LoomEditor.View/Comments/MarkdownToolbar.Blocks.cs` — create
- `tests/LoomEditor.View.Tests/Comments/MarkdownToolbarBlockTests.cs` — create; remove `Apply_BlockAction_ReturnsInputUnchanged` from the inline tests

## Design decisions (already made — do not change)

- **Affected lines** (Heading, lists, Quote): the lines touched by the selection, from the start of the line holding `Start` to the end of the line holding `End`; when the selection is **not empty** and `End` is at the very start of a line, that last line is **not** included.
- **Line prefixes:** heading `"### "`, bullet `"- "`, numbered `"{n}. "` (n counts the **non-blank** affected lines from 1), quote `"> "`.
- **Toggle:** if **every non-blank** affected line already has the prefix (for numbered: matches `digits + ". "`), remove it from all; otherwise add it to every non-blank line that lacks it (for numbered: a line that has another list prefix — `- ` or a different number — has that prefix **replaced**, and all lines are renumbered from 1). Blank lines are never touched. Heading: a line starting with one to six `#` and a space has that prefix **replaced** by `### ` (a different level is changed, `### ` is removed).
- **Selection after a line prefix edit:** non-empty selection → it covers the whole affected region (start of the first line to the end of the last line); empty selection → the caret keeps its position relative to the text of its line (it moves by the length of the prefix added or removed, never before the start of the line).
- **Code block:** the selection is replaced by a fenced block ``` on its own lines: `"```\n" + selectedText + "\n```"`; the block starts at a line start (a `\n` is added before it when the selection does not start at one) and is followed by a `\n` when text follows on the same line. Empty selection → `"```\n\n```"` with the caret on the empty middle line. Non-empty → the selection covers the inner text. Toggle: if the selected lines are exactly a fence, an inner text and a fence, the fences are removed.
- **Suggestion:** inserts `"```suggestion\n" + (suggestionSeed ?? "") + "\n```"` replacing the selection, at a line start (same newline rule as the code block). The new selection covers the seed text inside the block, so typing replaces it. `suggestionSeed == null` behaves as an empty seed. The seed is the content of the commented line (P11-08 supplies it). The seed must not contain `\n` (a `\n` in it is an `ArgumentException`). If the seed contains a line that starts with three backticks, the fence of the block is four backticks instead of three (rare; keep the rule, test it).
- Nothing else changes in the inline code.

## Tests

- [x] New (one test per bullet): `Heading_AddsPrefix`, `Heading_SamePrefix_Removes`, `Heading_OtherLevel_Replaces`, `Bullet_MultiLine_PrefixesNonBlankLines`, `Bullet_AllHavePrefix_Removes`, `Bullet_SomeHavePrefix_AddsToTheOthers`, `Bullet_SelectionEndsAtLineStart_ExcludesLastLine`, `Numbered_CountsNonBlankLines`, `Numbered_ReplacesBulletPrefix`, `Numbered_AllNumbered_Removes`, `Quote_AddsAndRemoves`, `LinePrefix_EmptySelection_CaretKeepsRelativePosition`, `CodeBlock_Selection_WrapsInFences`, `CodeBlock_Empty_CaretOnMiddleLine`, `CodeBlock_NotAtLineStart_AddsNewlineBefore`, `CodeBlock_Fenced_Removes`, `Suggestion_InsertsBlockWithSeedSelected`, `Suggestion_NoSeed_EmptyBlock`, `Suggestion_SeedWithNewline_Throws`, `Suggestion_SeedWithFence_UsesLongerFence`, `Suggestion_MidLine_StartsOnNewLine`
- [x] Round trip (`[Theory]`, with `MarkdownParser.Parse(text, Extended)`): bullet → `MarkdownList { Ordered: false }`, numbered → `Ordered: true`, quote → `MarkdownQuote`, heading → `MarkdownHeading { Level: 3 }`, code block → `MarkdownCodeBlock`, suggestion → `MarkdownCodeBlock { Language: "suggestion", Code: <seed> }`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The buttons and the text box (P11-17), the `\r` conversion of WinUI (done by the widget), keyboard shortcuts.

## Suggested commit message

`feat(view): add the Markdown toolbar block formats and the suggestion block`
