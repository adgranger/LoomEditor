# P4-29 — `TextModel` tokenization (1/2): language id, token backend, `TokensChanged`

- **Status:** todo
- **Depends on:** P4-28
- **Size:** L — split: `TextModel.Tokenization.cs`, `Model/Tokens/TokenizationTextModelPart.cs`, `Model/Tokens/TokenizerSyntaxTokenBackend.cs`

## Goal

A `TextModel` has a language id and tokens: it gets its tokenization support from the editor's
`TokenizationRegistry`, keeps tokens and states up to date when the text changes, raises `TokensChanged`, and reports
when background tokenization is finished.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/common/tokenizationTextModelPart.ts` | 14–102 | `ITokenizationTextModelPart` (the subset below), `BackgroundTokenizationState` |
| vscode: `src/vs/editor/common/model/tokens/tokenizationTextModelPart.ts` | 31–223, 358–389 | `TokenizationTextModelPart`: constructor (tokenizer backend only), `handleDidChangeContent`, `handleDidChangeAttached`, `getLineTokens` (without semantic tokens), `_emitModelTokensChangedEvent`, `hasTokens`, `resetTokenization`, `backgroundTokenizationState`, `forceTokenization`, `hasAccurateTokensForLine`, `isCheapToTokenize`, `tokenizeIfCheap`, `getTokenTypeIfInsertingCharacter`, `tokenizeLinesAt`, `getLanguageId`, `getLanguageIdAtPosition`, `setLanguageId` |
| vscode: `src/vs/editor/common/model/tokens/abstractSyntaxTokenBackend.ts` | 138–188 | `AbstractSyntaxTokenBackend` (without attached views: P4-30) |
| vscode: `src/vs/editor/common/model/tokens/tokenizerSyntaxTokenBackend.ts` | 26–222, 258–323 | `TokenizerSyntaxTokenBackend`: constructor, `todo_resetTokenization` (the store adapter: `setTokens`, `backgroundTokenizationFinished`, `setEndState` "renderer is ahead of worker"; `createBackgroundTokenizer` or `DefaultBackgroundTokenizer`; **not** the verify-only path), `handleDidChangeContent`, `forceTokenization`, `hasAccurateTokensForLine`, `isCheapToTokenize`, `getLineTokens`, `getTokenTypeIfInsertingCharacter`, `tokenizeLinesAt`, `hasTokens` |
| vscode: `src/vs/editor/common/textModelEvents.ts` | 140–152 | `IModelTokensChangedEvent` (without font tokens), plus `IModelLanguageChangedEvent` |
| vscode: `src/vs/editor/common/model/textModel.ts` | 230, 294, 360, 472 | `onDidChangeTokens`, `tokenization`, where the part is created, where content changes reach it |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModel.Tokenization.cs` — create (internal `Tokenization` property, internal constructor overload taking a `TokenizationRegistry`, `TokensChanged` and `LanguageChanged` events)
- `src/LoomEditor.Core/Model/Tokens/TokenizationTextModelPart.cs`, `TokenizerSyntaxTokenBackend.cs`, `BackgroundTokenizationState.cs` — create
- `src/LoomEditor.Core/Tokens/IBackgroundTokenizer.cs`, `IBackgroundTokenizationStore.cs` — already created in P4-28 (moved there: `DefaultBackgroundTokenizer` implements them). Here: add `CreateBackgroundTokenizer(TextModel, IBackgroundTokenizationStore)` (may return null) to `ITokenizationSupport` (moved from P4-23, which could not reference `TextModel` and `ContiguousMultilineTokens`)
- `src/LoomEditor.Core/Model/Events/ModelTokensChangedEventArgs.cs`, `ModelLanguageChangedEventArgs.cs` — create
- `tests/LoomEditor.Core.Tests/Model/Tokens/TextModelTokenizationTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0008 decision 4. Everything is **internal** in phase 4 (the public way to choose a language is the control's
  `Language` property, P4-45). The existing public constructors keep working: they use an empty registry (plain text).
- `setLanguageId(languageId)` looks the support up in the registry; the registry's `Changed` event resets tokenization
  when the model's language is in `ChangedLanguages`, or when the color map changed (Monaco listens to
  `TokenizationRegistry.onDidChange` for both).
- Content changes reach the part **after** the buffer change and **before** `ContentChanged` listeners, in Monaco's
  order (`textModel.ts` 472): find the matching place in `TextModel.Edits.cs`.
- Monaco's `observable` / `derived` (backend choice) is replaced by a direct field: there is only the tokenizer backend.
- The `TokensChanged` event carries the changed line ranges (`fromLineNumber`, `toLineNumber`).

## Tests

New (upstream tests come in P4-30 … P4-32):

- [ ] `SetLanguageId_UnknownLanguage_GivesNullTokens`
- [ ] `GetLineTokens_AfterForceTokenization_UsesTheSupport`
- [ ] `Edit_InvalidatesFollowingStates_AndRaisesTokensChanged`
- [ ] `RegistryChanged_ForTheModelLanguage_ResetsTokenization`
- [ ] `BackgroundTokenizationFinished_SetsCompleted`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (phase 1–3 model tests untouched)
- [ ] `docs/PORTING.md` map updated; "Behavior differences": no semantic tokens, tree-sitter, font tokens
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Attached views and the viewport (P4-30), bracket pairs (phase 5).

## Suggested commit message

`feat(core): tokenize the text model`
