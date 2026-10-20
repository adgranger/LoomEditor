# P5-49 — `LineCommentCommand`

- **Status:** todo
- **Depends on:** P5-48
- **Size:** M

## Goal

Port Monaco's line comment command (toggle, add or remove `//` on every selected line, aligned on the smallest
indentation; falls back to a block comment when the language has no line comment) and the first suite of its tests.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/comment/browser/lineCommentCommand.ts` | 18–48 | `IInsertionPoint`, `ILinePreflightData`, `IPreflightData…`, `ISimpleModel`, `const enum Type` |
| same | 50–80 | `class LineCommentCommand`: fields, constructor |
| same | 86–109 | `_gatherPreflightCommentStrings` |
| same | 115–190 | `_analyzeLines` |
| same | 195–205 | `_gatherPreflightData` |
| same | 210–234 | `_executeLineComments` |
| same | 236–284 | `_attemptRemoveBlockComment` |
| same | 289–334 | `_executeBlockComment` |
| same | 336–383 | `getEditOperations`, `computeCursorState` |
| same | 388–426 | `_createRemoveLineCommentsOperations`, `_createAddLineCommentsOperations` |
| same | 428–480 | `nextVisibleColumn`, `_normalizeInsertionPoint` |
| `contrib/comment/test/browser/lineCommentCommand.test.ts` | 22–35 | helper `createTestCommandHelper` |
| same | 37–707 | suite `Editor Contrib - Line Comment Command` (24 tests), with its helpers `createSimpleModel` (104), `createBasicLinePreflightData` (112) |

## Files to create or change

- `src/LoomEditor.View/Features/Comment/LineCommentCommand.cs` (+ `LineCommentCommand.Preflight.cs` if the file passes ~400 lines) — create
- `src/LoomEditor.View/Features/Comment/LineCommentType.cs` — create
- `tests/LoomEditor.View.Tests/Features/Comment/LineCommentCommandTests.cs` — create

## Design decisions (already made — do not change)

- `internal enum LineCommentType { Toggle = 0, ForceAdd = 1, ForceRemove = 2 }` (Monaco's `Type`; renamed so it never
  clashes with `System.Type`).
- `internal sealed class LineCommentCommand(LanguageConfigurationRegistry languageConfigurations, Selection selection, int indentSize, LineCommentType type, bool insertSpace, bool ignoreEmptyLines, bool ignoreFirstLine = false) : IEditCommand`.
- The preflight types are `internal sealed class LinePreflightData { bool Ignore; string CommentStr; int CommentStrOffset; int CommentStrLength; }`
  (mutable: `_normalizeInsertionPoint` writes `CommentStrOffset`) and
  `internal sealed record PreflightData(bool Supported, bool ShouldRemoveComments, List<LinePreflightData> Lines)`
  (`Supported = false` ⇔ Monaco's `IPreflightDataUnsupported`; then `Lines` is empty).
- `ISimpleModel` is not ported as an interface: the static methods that take it (`AnalyzeLines`,
  `NormalizeInsertionPoint`) take `Func<int, string> getLineContent`. The command passes `model.GetLineContent`; the
  tests pass `n => lines[n - 1]`.
- Static methods tested by Monaco are `internal static` without the underscore: `AnalyzeLines`,
  `GatherPreflightData`, `NormalizeInsertionPoint`, `CreateRemoveLineCommentsOperations`.
- `config?.lineCommentNoIndent ?? false` → `config?.LineCommentNoIndent ?? false` on step B's `CommentsConfiguration`
  (P5-14; `bool?`, from `new LineCommentConfig("!@#", NoIndent: true)`). `if (!commentStr)` →
  `string.IsNullOrEmpty(commentStr)`.
- `strings.firstNonWhitespaceIndex` → `StringClassification.FirstNonWhitespaceIndex`; `Constants.MAX_SAFE_SMALL_INTEGER`
  → a private const `MaxSafeSmallInteger = 1 << 30`, as in `MoveOperations.Horizontal.cs`.
- `lineContent.charCodeAt(j) === CharCode.Tab` reads inside the line (j < commentStrOffset): no out-of-range guard needed.
- Tests use step D's `CommandTester.TestCommand(…, languageId: "commentMode", prepare: …)` (P5-40, as in P5-48)
  with the comment rules of each helper (lines 41–54): `{ lineComment: '!@#', blockComment: ['<!@#', '#@!>'] }` and
  `{ lineComment: { comment: '!@#', noIndent: true }, … }`, registered in the `TestEditorServices`' registry. The
  `_analyzeLines` test registers the same configuration in a `new LanguageConfigurationRegistry()` and passes it.

## Tests

Port, suite `Editor Contrib - Line Comment Command` (24):

- [ ] `comment single line`, `case insensitive`, `comment with token column fixed`
- [ ] `_analyzeLines`, `_normalizeInsertionPoint`
- [ ] `detects indentation`, `detects mixed indentation`, `ignores whitespace lines`, `removes its own`, `works in only whitespace`
- [ ] `bug 9697 - whitespace before comment token`, `bug 10162 - line comment before caret`, `comment single line - leading whitespace`
- [ ] `ignores invisible selection`, `multiple lines`, `multiple modes on multiple lines`, `toggle single line`, `toggle multiple lines`
- [ ] `issue #5964: Ctrl+/ to create comment when cursor is at the beginning of the line puts the cursor in a strange position`
- [ ] `issue #35673: Comment hotkeys throws the cursor before the comment`
- [ ] `issue #2837 "Add Line Comment" fault when blank lines involved`, `issue #47004: Toggle comments shouldn't move cursor`
- [ ] `insertSpace false`, `insertSpace false does not remove space`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `contrib/comment/browser/lineCommentCommand.ts` → `Features/Comment/LineCommentCommand.cs`
      (✅, P5-49); tests table `lineCommentCommand.test.ts` 24 / 39 (the other 15 in P5-50)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The four other suites of `lineCommentCommand.test.ts` (P5-50), the actions (P5-51).

## Suggested commit message

`feat(view): port LineCommentCommand`
