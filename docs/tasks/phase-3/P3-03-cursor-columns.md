# P3-03 — `CursorColumns` and indentation helpers

- **Status:** done
- **Depends on:** P3-02
- **Size:** S

## Goal

Convert between a **column** (UTF-16 position in a line, starting at 1) and a **visible column** (where it is drawn:
a tab jumps to the next tab stop, full-width characters and emoji take 2), and compute tab stops. Every vertical
cursor move and every Tab key press uses this. Also port `normalizeIndentation`, used by Enter and Tab.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

If a function is not exactly at the given line, search for its name.

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/core/cursorColumns.ts` | 25–149 | `class CursorColumns`: `_nextVisibleColumn`, `visibleColumnFromColumn`, `toStatusbarColumn`, `columnFromVisibleColumn`, `nextRenderTabStop`, `nextIndentTabStop`, `prevRenderTabStop`, `prevIndentTabStop` |
| `src/vs/editor/common/core/misc/indentation.ts` | 9–41 | `_normalizeIndentationFromWhitespace`, `normalizeIndentation` |
| `src/vs/editor/test/common/core/cursorColumns.test.ts` | 9–281 | all tests |

## Files to create or change

- `src/LoomEditor.Core/Text/CursorColumns.cs` — create: `internal static class CursorColumns`
- `src/LoomEditor.Core/Text/Indentation.cs` — create: `internal static class Indentation` with `NormalizeIndentation(string str, int indentSize, bool insertSpaces)`
- `tests/LoomEditor.Core.Tests/Text/CursorColumnsTests.cs`, `IndentationTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- Port line by line; do not "improve" the algorithms:
  - `visibleColumnFromColumn` and `columnFromVisibleColumn` walk the line **one grapheme at a time** with
    `GraphemeIterator` (P3-02). For each grapheme they read the **first code point** with `Strings.GetNextCodePoint`
    and call `_nextVisibleColumn` once. So `e` + U+0301 counts as one visible column.
  - `toStatusbarColumn` walks **code points** with `CodePointIterator` (not graphemes) and counts each one as 1, tabs
    to the next tab stop.
- `_nextVisibleColumn` (private, `NextVisibleColumn`) uses `Strings.IsFullWidthCharacter` and
  `Strings.IsEmojiImprecise` (P3-01): +2 for those, +1 otherwise, next tab stop for `'\t'`.
- `columnFromVisibleColumn` returns a column that can be `lineContent.Length + 1` at most. It does **not** clamp to the
  model's min/max column: `CursorConfiguration.ColumnFromVisibleColumn` (P3-13) does that.
- `nextRenderTabStop(visibleColumn, tabSize)` = `visibleColumn + tabSize - visibleColumn % tabSize`. Keep the exact
  formulas of lines 122–148 (they use `Math.floor` on ints, which is integer division for non-negative values).
- `NormalizeIndentation` (lines 35–41): find the first non-whitespace index with the existing
  `StringClassification.FirstNonWhitespaceIndex` (`LoomEditor.Core/Model/`); when it is `-1`, use `str.Length`.
  Normalize the whitespace prefix with `NormalizeIndentationFromWhitespace` (lines 9–33: tabs count to the next
  indent tab stop, then `insertSpaces ? spaces : tabs + remaining spaces`) and append the rest of the string
  unchanged. Use a `StringBuilder` or `new string(' ', n)` instead of string concatenation in a loop.

## Tests

- [x] Port `cursorColumns.test.ts` › `visibleColumnFromColumn` (line 13), › `toStatusbarColumn` (72),
      › `columnFromVisibleColumn` (128), and the two generated loops (lines 204–281): one `[Theory]` per loop with the
      function name as data (`nextRenderTabStop`, `nextIndentTabStop`; `prevRenderTabStop`, `prevIndentTabStop`).
      Keep the `// Monaco: '<name>'` comments.
- [x] New: `NormalizeIndentation("\t  x", indentSize: 4, insertSpaces: true)` is `"      x"`;
      `NormalizeIndentation("        x", 4, insertSpaces: false)` is `"\t\tx"`; `NormalizeIndentation("x", 4, true)` is `"x"`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `editor/common/core/cursorColumns.ts` → `LoomEditor.Core/Text/CursorColumns.cs` (✅ ported, P3-03);
      `editor/common/core/misc/indentation.ts` → `Indentation.cs` (`normalizeIndentation` only); tests table row
      `editor/test/common/core/cursorColumns.test.ts`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`getIndentationLength`, indentation guessing (phase 4–5).

## Suggested commit message

`feat(core): port CursorColumns and normalizeIndentation`
