# P4-36 — Background tokenization (1/3): `MirrorTextModel`, `PrefixSumComputer`, `LengthEdit`, index transformer

- **Status:** todo
- **Depends on:** P4-35
- **Size:** M

## Goal

The pieces the background tokenizer needs exist in Core: a lightweight copy of the document that applies content
changes (the worker's model), and the length edits / index transformer that rebase line indices on later edits.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/common/model/mirrorTextModel.ts` | 12–197 | `IModelContentChange` (use the existing `ModelContentChange`), `IModelChangedEvent` (use the existing `ModelContentChangedEventArgs` data), `MirrorTextModel` |
| vscode: `src/vs/editor/common/model/prefixSumComputer.ts` | 9–187, 298–308 | `PrefixSumComputer`, `PrefixSumIndexOfResult` (not `ConstantTimePrefixSumComputer`) |
| vscode: `src/vs/editor/common/core/edits/lengthEdit.ts` | 12–130 | `LengthEdit`, `LengthReplacement`, with the members of `BaseEdit` / `BaseReplacement` (`src/vs/editor/common/core/edits/edit.ts`) that they use — read that file and port only those |
| vscode: `src/vs/editor/common/model/textModelStringEdit.ts` | 51–61 | `linesLengthEditFromModelContentChange` |
| vscode: `src/vs/workbench/services/textMate/browser/indexTransformer.ts` | 8–64 | `IIndexTransformer`, `MonotonousIndexTransformer`, `CombinedIndexTransformer` |
| vscode: `src/vs/workbench/services/textMate/test/browser/arrayOperation.test.ts` | 11–79 | `'array operation'` › `'simple'` |

## Files to create or change

- `src/LoomEditor.Core/Model/MirrorTextModel.cs`, `PrefixSumComputer.cs` — create
- `src/LoomEditor.Core/Text/LengthEdit.cs`, `LengthReplacement.cs` — create; `OffsetRange.cs` — change (members needed)
- `src/LoomEditor.Core/Model/Tokens/IndexTransformer.cs`, `ModelContentChangeEdits.cs` (`LinesLengthEditFromModelContentChange`) — create
- `tests/LoomEditor.Core.Tests/Model/MirrorTextModelTests.cs`, `PrefixSumComputerTests.cs`, `tests/LoomEditor.Core.Tests/Model/Tokens/ArrayOperationTests.cs` — create

## Design decisions (already made — do not change)

- `PrefixSumComputer` uses `uint[]` (`Uint32Array`), with Monaco's growth and validity index.
- `MirrorTextModel` is single-threaded (the worker's thread). It receives **copies**: an immutable record
  `MirrorModelChangedEvent(IReadOnlyList<ModelContentChange> Changes, string Eol, int VersionId)` built on the UI thread
  from `ModelContentChangedEventArgs`.
- `_acceptInsertText` splits lines with Monaco's `splitLines` semantics (`\r\n`, `\r`, `\n`): use the existing helper
  of Core if there is one (search), otherwise port `strings.splitLines` here.
- If Monaco has tests for these files (search `src/vs/editor/test/common/` for `prefixSumComputer.test.ts` and
  `mirrorTextModel.test.ts` at the pinned commit), port them; otherwise the new tests below.

## Tests

- [ ] Port: `'array operation'` › `'simple'`
- [ ] Port: the Monaco tests of `PrefixSumComputer` and `MirrorTextModel`, if they exist (list them in the commit body)
- [ ] New: `MirrorTextModel_AppliesChangesInOrder_AndTracksVersion`
- [ ] New: `MonotonousIndexTransformer_IndexInsideReplacedRange_IsUndefined`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map and ported-tests list updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The worker (P4-37).

## Suggested commit message

`feat(core): port the mirror text model and length edits`
