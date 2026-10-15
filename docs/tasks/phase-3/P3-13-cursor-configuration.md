# P3-13 — `CursorConfiguration` and the editor's cursor options

- **Status:** todo
- **Depends on:** P3-12
- **Size:** S

## Goal

`CursorConfiguration` is the bag of settings every cursor operation reads: tab size, page size, word separators,
read-only, multi-cursor options… Port it with the editor options it needs (with Monaco's defaults), and leave the
language-dependent fields for phases 4–5.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursorCommon.ts` | 51–168 | `class CursorConfiguration`: fields, constructor (**without** the language parts, see below) |
| same | 183–185 | `inputMode` (always `"insert"` in phase 3) |
| same | 199–201 | `normalizeIndentation` |
| same | 225–247 | `visibleColumnFromColumn`, `columnFromVisibleColumn` (clamps to the line's min/max column) |
| `src/vs/editor/common/config/editorOptions.ts` | see the table below | defaults of the editor options |

Editor options read by `CursorConfiguration` and their **defaults** (from `editorOptions.ts`; check each line):

| Option | Default | Line |
|---|---|---|
| `readOnly` | `false` | 6623 |
| `stickyTabStops` | `false` | 6201 |
| `useTabStops` | `true` | 6856 |
| `trimWhitespaceOnDelete` | `false` | 6835 |
| `wordSeparators` | `USUAL_WORD_SEPARATORS` | 6873 |
| `emptySelectionClipboard` | `true` | 6328 |
| `copyWithSyntaxHighlighting` | `true` | 6246 |
| `multiCursorMergeOverlapping` | `true` | 6507 |
| `multiCursorPaste` | `spread` | 6535 |
| `multiCursorLimit` | `10000` | 6547 |
| `autoClosingBrackets`, `autoClosingComments`, `autoClosingQuotes` | `languageDefined` | 6090, 6104, 6144 |
| `autoClosingDelete`, `autoClosingOvertype` | `auto` | 6118, 6131 |
| `autoSurround` | `languageDefined` | 6185 |
| `autoIndent` | `Full` (enum `None = 0, Keep = 1, Brackets = 2, Advanced = 3, Full = 4`, lines 42–48) | 6158 |
| `wordSegmenterLocales` | `[]` | 5546–5551 |
| `overtypeOnPaste` | `true` | 6580 |

## Files to create or change

- `src/LoomEditor.View/Cursors/EditorCursorOptions.cs` — create: `internal sealed record EditorCursorOptions` with the options above
- `src/LoomEditor.View/Cursors/EditorAutoClosingStrategy.cs`, `EditorAutoClosingEditStrategy.cs`,
  `EditorAutoSurroundStrategy.cs`, `EditorAutoIndentStrategy.cs`, `MultiCursorPasteMode.cs` — create (enums)
- `src/LoomEditor.View/Cursors/CursorConfiguration.cs` — create
- `tests/LoomEditor.View.Tests/Cursors/CursorConfigurationTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `EditorCursorOptions` is an `internal sealed record` whose property initializers are the defaults of the table
  (`static EditorCursorOptions Default { get; } = new();`). `WordSeparators` defaults to `WordHelper.UsualWordSeparators`
  (P3-04), `WordSegmenterLocales` to an empty array. String enums become C# enums:
  `EditorAutoClosingStrategy { Always, LanguageDefined, BeforeWhitespace, Never }`,
  `EditorAutoClosingEditStrategy { Always, Auto, Never }`,
  `EditorAutoSurroundStrategy { LanguageDefined, Quotes, Brackets, Never }`,
  `EditorAutoIndentStrategy { None = 0, Keep = 1, Brackets = 2, Advanced = 3, Full = 4 }`,
  `MultiCursorPasteMode { Spread, Full }`.
- Constructor: `CursorConfiguration(TextModelOptions modelOptions, EditorCursorOptions options, FontInfo fontInfo, EditorLayoutInfo layoutInfo)`.
  - `TabSize`, `IndentSize`, `InsertSpaces` from the model options (P3-09).
  - `LineHeight = fontInfo.LineHeight`, `TypicalHalfwidthCharacterWidth = fontInfo.TypicalHalfwidthCharacterWidth`.
  - `PageSize = Math.Max(1, (int)Math.Floor((double)layoutInfo.Height / LineHeight) - 2)` (line 129).
  - All other fields copied from `options`.
- **Not ported in phase 3** (phase 4–5 cut points): `autoClosingPairs`, `surroundingPairs`, `blockCommentStartToken`,
  `shouldAutoCloseBefore`, `electricChars`, `onElectricCharacter`, `_getShouldAutoClose`, the language id and the
  `languageConfigurationService`. Put one comment block in the class listing them. Phase 3 behaves as Monaco does for
  a model with no language configuration.
- `InputMode` returns `"insert"`: add `internal enum InputMode { Insert, Overtype }` and a property
  `InputMode InputMode => InputMode.Insert;` with a comment (overtype is out of phase 3, ADR 0006 decision 12; Monaco
  reads a global, which is not ported).
- `string NormalizeIndentation(string str)` → `Indentation.NormalizeIndentation(str, IndentSize, InsertSpaces)` (P3-03).
- `int VisibleColumnFromColumn(ICursorSimpleModel model, Position position)` and
  `int ColumnFromVisibleColumn(ICursorSimpleModel model, int lineNumber, int visibleColumn)` exactly as lines 225–247
  (they call `CursorColumns` from P3-03 and clamp to `GetLineMinColumn` / `GetLineMaxColumn`).
- `static bool ShouldRecreate(...)` is **not** ported: the view model recreates the configuration whenever an input
  changes (P3-20).

## Tests

- [ ] New: defaults — `PageSize` is `max(1, floor(height / lineHeight) - 2)` (height 600, line height 19 → 29;
      height 10 → 1), `WordSeparators == WordHelper.UsualWordSeparators`, `MultiCursorLimit == 10000`, `InputMode == Insert`
- [ ] New: `ColumnFromVisibleColumn` on `"\tab"` with tab size 4: visible column 4 → column 2; 100 → max column 4
- [ ] New: `VisibleColumnFromColumn` on `"\tab"` at column 3 → 5
- [ ] New: `NormalizeIndentation("\t x")` with `InsertSpaces = true`, tab size 4 → `"     x"`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `cursorCommon.ts` (`CursorConfiguration` without language parts, P3-13); behavior difference
      "`CursorConfiguration` has no language configuration in phase 3; `inputMode` is always insert"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Where the options come from at run time (the view model, P3-20; the control's `IsReadOnly`, P3-41).

## Suggested commit message

`feat(view): add CursorConfiguration and the cursor editor options`
