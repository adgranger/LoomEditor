# P5-30 — Bracket pair tree (9/9): `BracketPairsTree`

- **Status:** todo
- **Depends on:** P5-29
- **Size:** M (two partial files)

## Goal

`BracketPairsTree` exists: it builds the bracket pair AST of a `TextModel` (without tokens first, then with tokens when
background tokenization completes), queues text and token edits and reparses incrementally when it is read, and answers
the four tree queries: brackets in a range (with nesting levels for colorization), bracket pairs in a range (with
optional min indentation), first bracket before / after a position, and "is there an unmatched closing bracket after
this position".

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `common/model/bracketPairsTextModelPart/bracketPairsTree/bracketPairsTree.ts` (537 lines).

| Lines | What to port |
|---|---|
| 27–80 | `BracketPairsTree`: fields, `didLanguageChange`, `onDidChange`, constructor (the three cases: no tokens → `FastTokenizer`; tokenization completed → tree with tokens only; otherwise both trees from the text buffer) |
| 82–151 | `handleDidChangeBackgroundTokenizationState`, `handleDidChangeTokens`, `handleContentChanged`, `handleEdits`, `flushQueue`, `parseDocumentFromTextBuffer` |
| 153–204 | `getBracketsInRange`, `getBracketPairsInRange`, `hasUnmatchedClosingBracketAfter`, `getFirstBracketAfter`, `getFirstBracketBefore` |
| 206–236 | `hasUnmatchedClosingBracketAfter` (the free function) |
| 238–291 | `getFirstBracketBefore`, `getFirstBracketAfter` |
| 293–420 | `collectBrackets` |
| 422–537 | `CollectBracketPairsContext`, `collectBracketPairs` |

## Files to create or change

- `src/LoomEditor.Core/Model/BracketPairs/Tree/BracketPairsTree.cs` — create (class, events, queries; lines 27–236)
- `src/LoomEditor.Core/Model/BracketPairs/Tree/BracketPairsTree.Collect.cs` — create (partial: lines 238–537, private static)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/BracketPairsTreeTests.cs` — create
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/TestHelpers/BracketPairsTestModels.cs` — create: builds a `TextModel` with a language id, a `TokenizationRegistry`, a `LanguageIdCodec` and a `LanguageConfigurationRegistry` (P5-14) given to the `TextModel` constructor (P5-15) (port of the `createModelServices` + `instantiateTextModel` pattern of the bracket tests, and of `createTextModelWithBrackets`, `textModelWithTokens.test.ts` 23–33)

## Design decisions (already made — do not change)

- `BracketPairsTree(TextModel textModel, Func<string, ResolvedLanguageConfiguration> getLanguageConfiguration)`;
  Monaco's `Disposable` / `Emitter<void>` → `internal event EventHandler? Changed` (no `IDisposable`: the tree owns no
  resource). The two trees are `AstNode? _initialAstWithoutTokens` and `AstNode? _astWithTokens`; the two queues are
  `List<TextEditInfo>`.
- `textModel.tokenization.hasTokens`, `backgroundTokenizationState`, `hasAccurateTokensForLine` are the phase 4
  members of `TextModel.Tokenization`. `textModel.getLanguageId()` is `Tokenization.GetLanguageId()`
  equivalent).
- `handleDidChangeTokens({ ranges })` takes `ModelTokensChangedEventArgs` (`Ranges`: `FromLineNumber`,
  `ToLineNumber`). `handleContentChanged(change)` takes `ModelContentChangedEventArgs` and uses
  `TextEditInfo.FromModelContentChanges(change.Changes)`.
- `parseDocumentFromTextBuffer`: `isPure` is the constant `false` in Monaco; port only the `false` branch (no deep
  clone) and keep Monaco's comment.
- `getBracketsInRange(range, onlyColorizedBrackets)` and `getBracketPairsInRange(range, includeMinIndentation)` return
  a `CallbackIterable<…>` that flushes nothing itself: `flushQueue` runs when the method is called, the walk runs when
  the iterable is iterated (Monaco's order). `new Map()` per walk → a new `Dictionary<string, int>` per walk.
- **Recursion.** `collectBrackets` and `collectBracketPairs` keep Monaco's recursion and their `level > 200` guard.
  In `collectBrackets`, `continue whileLoop` jumps out of a `for` inside a `switch`: write it as
  `node = child; descend = true; break;` out of the `for`, then `continue` the outer `while (true)` when `descend` is
  set. Keep every condition of Monaco in the same order. No `goto`.
- `(node.closingBracket.bracketInfo as ClosingBracketKind).closesColorized(node.openingBracket.bracketInfo as
  OpeningBracketKind)` uses step B's `ClosingBracketKind.ClosesColorized(OpeningBracketKind)`.
- `hasUnmatchedClosingBracketAfter` keeps its explicit stack (`List<(AstNode Node, Length Offset)>`), not recursion.
- `computeMinIndentation` is called only when `includeMinIndentation` is true (guides: phase 6). Port it anyway.
- Threading: UI thread only (it reads the model).

## Tests

The upstream tests of this class go through the model part (`getBracketPairsInRange.test.ts`, P5-31). Here, new tests
on the tree alone, with `BracketPairsTestModels`:

- [ ] New: `ModelWithoutTokens_UsesFastTokenizer_FindsPairs` (`"{ [ ( ) ] }"`, `GetBracketPairsInRange` over the whole
      text gives three pairs sorted by start, nesting levels 0, 1, 2)
- [ ] New: `GetBracketsInRange_OnlyColorized_SkipsPairsNotColorized` (with `colorizedBracketPairs` smaller than
      `brackets`)
- [ ] New: `GetFirstBracketBefore_And_After_SkipUnexpectedClosingBrackets`
- [ ] New: `ContentChanged_ThenQuery_ReparsesIncrementally` and the randomized
      `IncrementalReparse_EqualsFreshTree` (seeded `[Theory]` 1…30, seed in the message: 20 random edits of a text made
      of brackets, letters and newlines, each followed by a query; after each edit the brackets of the tree equal the
      brackets of a new `BracketPairsTree` built on a copy of the model)
- [ ] New: `TokensChanged_FiresChanged_WhenNoInitialTree`
- [ ] New: `BackgroundTokenizationCompleted_DropsInitialTree_AndFiresChanged`
- [ ] New: `HasUnmatchedClosingBracketAfter_TokensNotAccurate_ReturnsFalse`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `bracketPairsTree.ts` ✅ (P5-30); the whole `bracketPairsTree/` folder ✅
- [ ] Card status and `docs/tasks/README.md` updated
- [ ] `docs/tasks/phase-5/README.md` section 5: row for `BracketPairsTestModels` (created by P5-30)

## Out of scope

Hooking the tree into `TextModel` and the public bracket API (P5-31). The legacy (no-tree) code paths (P5-32,
P5-33).

## Suggested commit message

`feat(core): port the bracket pairs tree`
