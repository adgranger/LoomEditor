# P10-22 — Snippet variables: the resolvers

- **Status:** done
- **Depends on:** P10-21
- **Size:** M

## Goal

`$TM_SELECTED_TEXT`, `$TM_CURRENT_LINE`, `$CURRENT_YEAR`, `$LINE_COMMENT`, `$CLIPBOARD`, `$UUID` and the other known variables resolve to their values, with the selection-based multi-line re-indentation of Monaco; variables that need the host (file path, workspace, clipboard) go through one small interface.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/snippet/browser/snippetVariables.ts` | 20–60 | `KnownSnippetVariableNames` (38 names) |
| `src/vs/editor/contrib/snippet/browser/snippetVariables.ts` | 62–78 | `CompositeSnippetVariableResolver` |
| `src/vs/editor/contrib/snippet/browser/snippetVariables.ts` | 79–160 | `SelectionBasedVariableResolver` (`SELECTION`, `TM_SELECTED_TEXT` with the multi-line indentation fix, `TM_CURRENT_LINE`, `TM_CURRENT_WORD`, `TM_LINE_INDEX`, `TM_LINE_NUMBER`, `CURSOR_INDEX`, `CURSOR_NUMBER`) |
| `src/vs/editor/contrib/snippet/browser/snippetVariables.ts` | 161–207 | `ModelBasedVariableResolver` (`TM_FILENAME`, `TM_FILENAME_BASE`, `TM_DIRECTORY`, `TM_DIRECTORY_BASE`, `TM_FILEPATH`, `RELATIVE_FILEPATH`) |
| `src/vs/editor/contrib/snippet/browser/snippetVariables.ts` | 208–244 | `ClipboardBasedVariableResolver` (`CLIPBOARD`, with the `TM_...` line-count rule: read it) |
| `src/vs/editor/contrib/snippet/browser/snippetVariables.ts` | 245–269 | `CommentBasedVariableResolver` (`LINE_COMMENT`, `BLOCK_COMMENT_START`, `BLOCK_COMMENT_END`) |
| `src/vs/editor/contrib/snippet/browser/snippetVariables.ts` | 270–326 | `TimeBasedVariableResolver` (`CURRENT_*`) |
| `src/vs/editor/contrib/snippet/browser/snippetVariables.ts` | 329–378 | `WorkspaceBasedVariableResolver` (`WORKSPACE_NAME`, `WORKSPACE_FOLDER`) |
| `src/vs/editor/contrib/snippet/browser/snippetVariables.ts` | 379–393 | `RandomBasedVariableResolver` (`RANDOM`, `RANDOM_HEX`, `UUID`) |
| `src/vs/editor/contrib/snippet/test/browser/snippetVariables.test.ts` | whole file | the 17 tests |

## Files to create or change

- `src/LoomEditor.Core/Snippets/SnippetVariables.cs` (`KnownSnippetVariableNames`), `CompositeVariableResolver.cs`, `SelectionBasedVariableResolver.cs`, `ModelBasedVariableResolver.cs`, `ClipboardBasedVariableResolver.cs`, `CommentBasedVariableResolver.cs`, `TimeBasedVariableResolver.cs`, `WorkspaceBasedVariableResolver.cs`, `RandomBasedVariableResolver.cs`, `ISnippetVariableHost.cs` — create (small files; group the three tiny ones in `SimpleVariableResolvers.cs` if you prefer; stay under ~400 lines per file)
- `tests/LoomEditor.Core.Tests/Snippets/SnippetVariablesTests.cs` — create

## Design decisions (already made — do not change)

- **The host interface** (ADR 0013 decision 10): `internal interface ISnippetVariableHost { string? FilePath { get; } string? RelativeFilePath { get; } string? WorkspaceName { get; } string? WorkspaceFolder { get; } string? ReadClipboardText(); }` and `static ISnippetVariableHost None` (all `null`). `ModelBasedVariableResolver` derives `TM_FILENAME`, `TM_FILENAME_BASE`, `TM_DIRECTORY`, `TM_DIRECTORY_BASE`, `TM_FILEPATH` from `FilePath` with `System.IO.Path` (`GetFileName`, `GetFileNameWithoutExtension`, `GetDirectoryName`, the last segment of the directory); `RELATIVE_FILEPATH` from `RelativeFilePath`; every value `null` when the host gives `null`. Monaco's `IReadClipboardText` is `ReadClipboardText`.
- `SelectionBasedVariableResolver` takes `(TextModel model, Selection selection, int selectionIndex)`; the `OvertypingCapturer` parameter and the branch that uses it are **not ported** (that is the suggest widget's overtype tracking; keep a `// not ported: OvertypingCapturer` comment). The indentation fix uses `StringClassification` / the existing leading-whitespace helper of Core (`grep -rn "GetLeadingWhitespace" src`); `splitLines` is `Split` on `\r\n|\r|\n`; `commonPrefixLength` is a local helper.
- `TimeBasedVariableResolver(TimeProvider timeProvider)` (**not** `DateTime.Now`: tests pass a `FakeTimeProvider`-like subclass of `TimeProvider`, a tiny test double in `TestHelpers/`): local time `GetLocalNow()`. Names (`dayNames`, `monthNames`, short forms) are **English invariant** (`CultureInfo.InvariantCulture.DateTimeFormat`: `DayNames`, `AbbreviatedDayNames`, `MonthNames`, `AbbreviatedMonthNames`; Monaco localizes them: documented difference). `CURRENT_TIMEZONE_OFFSET` is `+hh:mm` / `-hh:mm` (lines 312–319), `CURRENT_TIMEZONE_NAME` is `TimeZoneInfo.Local.Id`.
- `RandomBasedVariableResolver(Random random)`: `RANDOM` is 6 random decimal digits, `RANDOM_HEX` 6 random hex digits, `UUID` `Guid.NewGuid().ToString()` lower case (injectable `Func<Guid>`). Tests pass `new Random(seed)` (never `Random.Shared`; the seed goes in assertion messages).
- `CommentBasedVariableResolver(TextModel model, Position position)` reads the comment rules from the language configuration at the position (`LanguageConfigurationRegistry`: `grep -rn "CommentRule\|GetComments" src/LoomEditor.Core/Languages`): `LINE_COMMENT` → `LineComment`, `BLOCK_COMMENT_START` → `BlockComment.Open`, `…_END` → `Close`; `null` when the language has none.
- `WorkspaceBasedVariableResolver(ISnippetVariableHost host)`: `WORKSPACE_NAME`, `WORKSPACE_FOLDER` straight from the host (no URI handling).
- `CompositeVariableResolver(params IVariableResolver[])` returns the first non-`null` result (Monaco: the first `!== undefined`: a resolver that returns the empty string **wins**; keep that).
- `<remarks>Threading: UI thread only; resolvers are created per insertion.</remarks>`.

## Tests

- [x] Port: `snippetVariables.test.ts` (names in `// Monaco:` comments). Tests that need a `ResolverService` / `IWorkspaceContextService` mock become tests of `ISnippetVariableHost` doubles; tests about localized month names expect the English ones
- [x] New: `Time_CurrentYear_Short_Month_Date_Hour_Minute_Second_Millisecond_PaddedTwoOrThree` (one test per value with a fixed `TimeProvider`), `Time_TimezoneOffset_Positive_And_Negative`, `Time_Unix_Seconds_And_Millis`, `Random_Is6Digits`, `RandomHex_Is6HexDigits`, `Uuid_IsAGuid`, `Composite_FirstNonNullWins`, `Composite_EmptyStringWins`, `Model_NoHost_AllNull`, `Selection_Multiline_ReindentsAgainstVariableIndent`, `Selection_Empty_ReturnsNull`, `Comment_NoCommentRules_Null`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `snippetVariables.ts` row; "Behavior differences": English names, host interface, no overtyping capture
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

A real host implementation (the control provides one in P10-25), `OvertypingCapturer`.

## Suggested commit message

`feat(core): port the snippet variable resolvers`
