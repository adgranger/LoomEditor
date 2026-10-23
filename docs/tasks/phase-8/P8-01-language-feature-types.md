# P8-01 — Language feature contracts: completion, hover, definition and `MarkdownString` (public API of Core)

- **Status:** done
- **Depends on:** P7-26
- **Size:** M (≈250 lines, mostly small types with XML doc)

## Goal

`LoomEditor.Core` has the **public** data types and provider interfaces that language features speak (Monaco `languages.ts`): an application, or the LSP client of step F,
can implement `ICompletionItemProvider`, `IHoverProvider` and `IDefinitionProvider`. No registry, no editor code yet (P8-02 and later).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/languages.ts` | 353–382 | `CompletionItemKind` (same members, same order: `Method` = 0 … `Snippet` = 28) |
| `src/vs/editor/common/languages.ts` | 524–560 | `CompletionItemLabel` (flattened, see decisions), `CompletionItemTag`, `CompletionItemRanges` |
| `src/vs/editor/common/languages.ts` | 558–661 | `CompletionItem`, `CompletionList` |
| `src/vs/editor/common/languages.ts` | 681–735 | `CompletionTriggerKind`, `CompletionContext`, `CompletionItemProvider` |
| `src/vs/editor/common/languages.ts` | 189–224 | `Hover`, `HoverProvider` |
| `src/vs/editor/common/languages.ts` | 1490–1560 | `Location`, `LocationLink`, `Definition`, `DefinitionProvider` |
| `src/vs/base/common/htmlContent.ts` | 17–19, 73–82, 116–125, 153–158 | `IMarkdownString.value` / `isTrusted`, `MarkdownString.appendText`, `isEmptyMarkdownString`, `escapeMarkdownSyntaxTokens` |

## Files to create or change

All in `src/LoomEditor.Core/Languages/Features/` (namespace `LoomEditor.Core.Languages.Features`), **public**, one type per file:

- `MarkdownString.cs`
- `CompletionItemKind.cs`, `CompletionItemTag.cs`, `CompletionRange.cs`, `CompletionItem.cs`, `CompletionList.cs`
- `CompletionTriggerKind.cs`, `CompletionContext.cs`, `ICompletionItemProvider.cs`
- `Hover.cs`, `IHoverProvider.cs`
- `Location.cs`, `LocationLink.cs`, `IDefinitionProvider.cs`
- `README.md` — what the folder is (one paragraph + the file list)
- `src/LoomEditor.Core/PublicAPI.Unshipped.txt` — regenerate with `./eng/Update-PublicApi.ps1 src/LoomEditor.Core`
- `tests/LoomEditor.Core.Tests/Languages/Features/MarkdownStringTests.cs`, `CompletionItemTests.cs` — create

## Design decisions (already made — do not change)

- Every type has XML doc and a `<remarks>Threading: …</remarks>` line. Data types are immutable **except** `CompletionItem` (see below).
- `public sealed record MarkdownString(string Value, bool IsTrusted = false)`: `Value` is **always Markdown source** (Monaco's `supportThemeIcons`, `supportHtml`, `baseUri`, `uris` are not ported).
  Members: `public bool IsEmpty => Value.Length == 0;` `public static bool IsNullOrEmpty(MarkdownString? value)` (true for null or empty; port of `isEmptyMarkdownString` for one value);
  `public static bool IsNullOrEmpty(IReadOnlyList<MarkdownString>? values)` (true for null, an empty list, or when **every** value is empty);
  `public static MarkdownString FromText(string text)`: port of `appendText` with the `Paragraph` newline style on an empty string: `escapeMarkdownSyntaxTokens(text)` then each run of spaces / tabs
  becomes `&nbsp;` repeated once per character, then every `>` becomes `\>`, then every `\n` becomes `\n\n`. `escapeMarkdownSyntaxTokens`: backslash-escape each of `\ ` `` ` `` `*` `_` `{` `}` `[` `]` `(` `)` `#` `+` `!` `~`, then
  escape a `-` that is the first non-blank character of a line (`^([ \t]*)-` per line → `$1\-`). It is a `private static` helper of the record (not public). Code (not regex-literal `lastIndex` tricks): a `[GeneratedRegex]` for the second rule is fine.
  A `\r` in the text is treated as part of the text (LSP servers send `\n`; `FromText` is not meant for `\r\n`: document that in the XML doc).
- `public enum CompletionItemKind` — exactly Monaco's names and order: `Method`, `Function`, `Constructor`, `Field`, `Variable`, `Class`, `Struct`, `Interface`, `Module`, `Property`, `Event`, `Operator`, `Unit`, `Value`, `Constant`,
  `Enum`, `EnumMember`, `Keyword`, `Text`, `Color`, `File`, `Reference`, `Customcolor`, `Folder`, `TypeParameter`, `User`, `Issue`, `Tool`, `Snippet` (0 … 28; **not** the LSP numbering: the LSP mapping is P8-30).
- `public enum CompletionItemTag { Deprecated = 1 }`.
- `public readonly record struct CompletionRange(Range Insert, Range Replace)` with `public static CompletionRange FromRange(Range range) => new(range, range);` (Monaco: `IRange | CompletionItemRanges`; a single range is insert = replace).
  (The factory was first called `Single`; renamed to `FromRange` because analyzer CA1720 rejects identifiers named after a type.)
- `public sealed class CompletionItem` (mutable on purpose: Monaco's `resolveCompletionItem` result is merged into the item with `Object.assign`). Properties, all `{ get; set; }`: `string Label` (required, non-empty for a valid item),
  `string? LabelDetail`, `string? LabelDescription` (Monaco's `CompletionItemLabel.detail` / `.description`, flattened), `CompletionItemKind Kind`, `IReadOnlyList<CompletionItemTag>? Tags`, `string? Detail`, `MarkdownString? Documentation`,
  `string? SortText`, `string? FilterText`, `bool Preselect`, `string InsertText` (required; **always plain text**: snippets are not supported, ADR 0011 decision 7), `CompletionRange? Range` (null = the provider wants the default range, filled in by P8-14),
  `IReadOnlyList<char>? CommitCharacters`, `IReadOnlyList<TextEdit>? AdditionalTextEdits` (`LoomEditor.Core.Text.TextEdit`), `object? ProviderData` (opaque, owned by the provider: an LSP provider keeps the raw item there for `completionItem/resolve`).
  Constructor `CompletionItem(string label, string insertText, CompletionItemKind kind = CompletionItemKind.Text)`; both strings are checked for null (`ArgumentNullException`), the empty label is allowed (it makes the item invalid later, P8-14).
  `public void MergeFrom(CompletionItem resolved)`: copies **only the non-null** `Detail`, `Documentation`, `AdditionalTextEdits`, `LabelDetail`, `LabelDescription`, `Tags` of `resolved` (what LSP can resolve); never `Label`, `InsertText`, `Range`, `SortText`, `FilterText`, `ProviderData`.
- `public sealed class CompletionList(IReadOnlyList<CompletionItem> suggestions, bool incomplete = false)`: `Suggestions`, `Incomplete`. Immutable.
- `public enum CompletionTriggerKind { Invoke = 0, TriggerCharacter = 1, TriggerForIncompleteCompletions = 2 }`;
  `public readonly record struct CompletionContext(CompletionTriggerKind TriggerKind, string? TriggerCharacter = null)`.
- `public interface ICompletionItemProvider`:
  `IReadOnlyList<string>? TriggerCharacters { get; }`,
  `Task<CompletionList?> ProvideCompletionItemsAsync(TextModel model, Position position, CompletionContext context, CancellationToken token);`,
  `bool CanResolve => false;` (default interface member), `Task<CompletionItem?> ResolveCompletionItemAsync(CompletionItem item, CancellationToken token) => Task.FromResult<CompletionItem?>(null);`.
  The contract (XML doc): called on the UI thread; the `Task` may complete on any thread; a result for a cancelled token is ignored by the caller; the provider must not touch the model after the task completed.
- `public sealed record Hover(IReadOnlyList<MarkdownString> Contents, Range? Range = null)`; `public interface IHoverProvider { Task<Hover?> ProvideHoverAsync(TextModel model, Position position, CancellationToken token); }`.
- `public readonly record struct Location(string Uri, Range Range)`; `public sealed record LocationLink(string Uri, Range Range, Range? OriginSelectionRange = null, Range? TargetSelectionRange = null)`
  (Monaco's `Definition = Location | Location[] | LocationLink[]` is **flattened to `IReadOnlyList<LocationLink>`**: a `Location` becomes a `LocationLink` with both optional ranges null;
  `Location` stays as a type for providers that want it and has `public LocationLink ToLink() => new(Uri, Range)`).
  `public interface IDefinitionProvider { Task<IReadOnlyList<LocationLink>?> ProvideDefinitionAsync(TextModel model, Position position, CancellationToken token); }`.
- `Range` is `LoomEditor.Core.Text.Range` (Core has no global alias: each file that uses it declares `using Range = LoomEditor.Core.Text.Range;`); `Position` is `LoomEditor.Core.Text.Position`; `TextModel` is `LoomEditor.Core.Model.TextModel`.
- File header (`// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (…)` + upstream commit line) on every file that ports a Monaco declaration; `README.md` has none.

## Tests

- [x] New (`MarkdownStringTests`; each comment names the Monaco function: `// Monaco: MarkdownString.appendText`): `FromText_PlainText_Unchanged`, `FromText_EscapesSyntaxTokens` (`"a*b_c"` → `"a\\*b\\_c"`), `FromText_SpacesBecomeNbsp` (`"a  b"` → `"a&nbsp;&nbsp;b"`, tab → one `&nbsp;`),
      `FromText_GreaterThanEscaped`, `FromText_NewlineBecomesParagraph` (`"a\nb"` → `"a\n\nb"`), `FromText_LeadingDashEscaped` (`"- x"` → `"\\-&nbsp;x"`: the dash is escaped, the space after it is `&nbsp;`; and `"a-b"` is unchanged),
      `IsNullOrEmpty_One`, `IsNullOrEmpty_List_EveryEmpty` (`null`, `[]`, `[""]`, `["", ""]` are empty; `["", "x"]` is not)
- [x] New (`CompletionItemTests`): `Constructor_NullLabel_Throws`, `Constructor_Defaults` (Kind `Text`, `Range` null, `Preselect` false), `MergeFrom_CopiesOnlyResolvableNonNullMembers` (detail, documentation, additional edits copied; label, insert text, sort text untouched; a null in `resolved` does not erase a value),
      `MergeFrom_Null_Throws`, `CompletionRange_FromRange_SameRange`, `Location_ToLink_NoOptionalRanges`, `CompletionItemKind_Order` (`Method` = 0, `EnumMember` = 16, `Snippet` = 28)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `./eng/Update-PublicApi.ps1 src/LoomEditor.Core` run; the diff of `PublicAPI.Unshipped.txt` lists exactly the new public members
- [x] `docs/PORTING.md` map: rows for `languages.ts` (the declarations above, with "not ported: `insertTextRules`, `command`, `action`, `extensionId`, `_id`, `dispose` of the list, `duration`, hover verbosity, `Declaration`/`Implementation`/`TypeDefinition`/`Reference` providers") and `htmlContent.ts` (`MarkdownString`: `appendText`, `isEmptyMarkdownString`, `escapeMarkdownSyntaxTokens`)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The registry (P8-02), anything that calls a provider, snippets, `Declaration` / `Implementation` / `TypeDefinition` / `Reference` providers, `CompletionItem.command`.

## Suggested commit message

`feat(core): add the language feature contracts (completion, hover, definition) and MarkdownString`
