# P8-02 — `LanguageFeatureRegistry<T>` and `LanguageFeaturesService`

- **Status:** done
- **Depends on:** P8-01
- **Size:** S

## Goal

An editor has a registry per feature (completion, hover, definition) where providers are registered for a language id (or `*`) and found, ordered by how well they match
(Monaco `languageFeatureRegistry.ts`), and one public service object that holds the three registries.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/languageFeatureRegistry.ts` | 14–22, 69–115 (`register`, `has`, `all`, `allNoModel`), 117–153 (`ordered`, `orderedGroups`, `_orderedForEach`), 155–213 (`_updateScores`, `_compareByScoreAndTime`) | `LanguageFeatureRegistry` |
| `src/vs/editor/common/languageSelector.ts` | 29–60 | `score` for a **string** selector (`'*'` → 5, the language id → 10, otherwise 0) |

## Files to create or change

- `src/LoomEditor.Core/Languages/Features/LanguageFeatureRegistry.cs` — create (**public**)
- `src/LoomEditor.Core/Languages/Features/LanguageFeaturesService.cs` — create (**public**)
- `src/LoomEditor.Core/Languages/Features/README.md` — change: add the two files
- `src/LoomEditor.Core/PublicAPI.Unshipped.txt` — regenerate (`./eng/Update-PublicApi.ps1 src/LoomEditor.Core`)
- `tests/LoomEditor.Core.Tests/Languages/Features/LanguageFeatureRegistryTests.cs`, `LanguageFeaturesServiceTests.cs` — create
- `tests/LoomEditor.Core.Tests/Languages/Features/TestHelpers/TestProviders.cs` — create (shared helper, README §6)

## Design decisions (already made — do not change)

- `public sealed class LanguageFeatureRegistry<T> where T : class`. A selector is a `string`: a language id, or `"*"`. Not ported: `LanguageFilter` objects (`scheme`, `pattern`, `exclusive`, `isBuiltin`, `notebookType`), notebooks, `NotebookInfoResolver`,
  `shouldSynchronizeModel`, `MatchCandidate` caching (`_lastCandidate`): the registry **does not take a model**, every query takes the **language id** (`string`) and recomputes (it is a handful of entries).
- Members: `IDisposable Register(string selector, T provider)` (null checks → `ArgumentNullException`; an empty selector → `ArgumentException`), `event EventHandler<int>? Changed` (the number of entries, Monaco `onDidChange`, raised after a registration and after a disposal that really removed an entry),
  `bool Has(string languageId)`, `IReadOnlyList<T> All(string languageId)`, `IReadOnlyList<T> Ordered(string languageId)` (the same list as `All`: Monaco's `all` also iterates the sorted entries; keep both names),
  `IReadOnlyList<IReadOnlyList<T>> OrderedGroups(string languageId)` (providers with equal score in one group, groups in descending score), `IReadOnlyList<T> AllNoModel()` (registration order).
- Score: `selector == languageId` → 10; `selector == "*"` → 5; else 0 (the language id comparison is `StringComparison.Ordinal`). Entries with score 0 are never returned. Order: score descending, then **most recently registered first** (Monaco `_compareByScoreAndTime`: `_time` descending; the `isBuiltin` rule is dropped).
- Disposing the result of `Register` twice removes nothing the second time and does not raise `Changed`.
- `public sealed class LanguageFeaturesService`: `LanguageFeatureRegistry<ICompletionItemProvider> CompletionProviders { get; }`, `LanguageFeatureRegistry<IHoverProvider> HoverProviders { get; }`, `LanguageFeatureRegistry<IDefinitionProvider> DefinitionProviders { get; }`
  (Monaco: `completionProvider`, `hoverProvider`, `definitionProvider` of `ILanguageFeaturesService`). Nothing else.
- `<remarks>Threading: UI thread only (registration and queries); the providers' tasks are not the registry's concern.</remarks>`
- Test helper `TestProviders.cs` (`internal`, namespace `LoomEditor.Core.Tests.Languages.Features.TestHelpers`):
  `FakeCompletionProvider(IReadOnlyList<string>? triggerCharacters = null)` with `Func<TextModel, Position, CompletionContext, CompletionList?>? OnProvide`, `List<(Position Position, CompletionContext Context)> Calls`, `CanResolve` settable + `Func<CompletionItem, CompletionItem?>? OnResolve`;
  `FakeHoverProvider(Func<TextModel, Position, Hover?>? onProvide)`; `FakeDefinitionProvider(Func<TextModel, Position, IReadOnlyList<LocationLink>?>? onProvide)`. Each returns `Task.FromResult(...)` synchronously and records its calls (`Calls`).
  Each has a `TaskCompletionSource`-based variant: `FakeCompletionProvider.Pending()` returns a provider whose calls return a task the test completes with `Complete(list)` (used by the async cards).

## Tests

- [x] New (comments name the Monaco member: `// Monaco: LanguageFeatureRegistry.ordered`): `Register_ThenAll_ReturnsProvider`, `Register_Dispose_RemovesProvider`, `Register_DisposeTwice_RaisesChangedOnce`, `Changed_ReportsEntryCount`,
      `Ordered_ExactLanguageBeforeStar`, `Ordered_SameScore_NewestFirst`, `Ordered_OtherLanguage_NotReturned`, `Ordered_StarOnly_MatchesAnyLanguage`, `Has_NoMatch_False`, `OrderedGroups_GroupsEqualScores` (two for the language, one `*`, one for the language registered after → `[[newest, older], [star]]`),
      `AllNoModel_RegistrationOrder`, `Register_NullProvider_Throws`, `Register_EmptySelector_Throws`
- [x] New (`LanguageFeaturesServiceTests`): `Registries_AreIndependent`, `Registries_AreTypedPerFeature` (registering a hover provider does not appear in `CompletionProviders`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `./eng/Update-PublicApi.ps1 src/LoomEditor.Core` run, diff checked
- [x] `docs/PORTING.md` map: `languageFeatureRegistry.ts`, `languageSelector.ts` (string selectors only) and `services/languageFeatures.ts` (three registries)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`EditorContext` / control wiring (P8-05), language filters and notebooks, other feature registries (code actions, references, …).

## Suggested commit message

`feat(core): add the language feature registry and service`
