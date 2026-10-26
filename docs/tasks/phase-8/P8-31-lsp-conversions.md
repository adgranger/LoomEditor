# P8-31 — Converting LSP types to the editor's types (`LspConvert`)

- **Status:** done
- **Depends on:** P8-30
- **Size:** M

## Goal

Everything that maps an LSP value to a `LoomEditor.Core` value is in one pure class, `LspConvert`: diagnostics become `MarkerData`, LSP completion items become `CompletionItem`s (kind, text, ranges, documentation, edits, commit characters), hovers become `Hover`s, locations become `LocationLink`s, and snippet text becomes plain text.

## Monaco sources

Sources: none (specified in this card). The mapping follows what VS Code's language client does (`vscode-languageclient` `protocolConverter`): the rules below are the decisions; ADR 0011 decision 5 for positions.

## Files to create or change

- `src/LoomEditor.Lsp/Editor/LspConvert.cs` (change: P8-30 created it), `LspConvert.Diagnostics.cs`, `LspConvert.Completion.cs`, `LspConvert.Hover.cs`, `LspConvert.Locations.cs`, `LspSnippet.cs` — create (`internal static`)
- `tests/LoomEditor.Lsp.Tests/Editor/LspConvertDiagnosticsTests.cs`, `LspConvertCompletionTests.cs`, `LspConvertHoverTests.cs`, `LspConvertLocationsTests.cs`, `LspSnippetTests.cs` — create

## Design decisions (already made — do not change)

- All members are `internal static` and pure. **Threading rule (ADR 0011 decision 4):** the conversions of completion items, hovers and locations run on a **thread-pool thread** (the provider continuations of P8-33 / P8-34), where the `TextModel` must not be touched: they take **no model** and convert ranges numerically with `ToRangeUnvalidated` (below); the editor code that consumes them validates the ranges on the UI thread (`SuggestItem`, `ContentHoverComputer`, `GotoDefinitionController` already do).
  Only `ToMarker` (called on the UI thread by P8-32) takes a `TextModel` and validates. `<remarks>` of each member says which kind it is. Add to `LspConvert` (P8-30 created it): `static Range ToRangeUnvalidated(LspRange range)` → `new Range(Math.Max(range.Start.Line, 0) + 1, Math.Max(range.Start.Character, 0) + 1, Math.Max(range.End.Line, 0) + 1, Math.Max(range.End.Character, 0) + 1)`, **swapped** when the end is before the start.
- **Severity** (`MarkerSeverity ToSeverity(int? severity)`): `1` → `Error`, `2` → `Warning`, `3` → `Info`, `4` → `Hint`, `null` or any other value → `Error` (the server did not say: the safe choice, same as VS Code).
- **`MarkerData ToMarker(LspDiagnostic diagnostic, TextModel model, int? documentVersion)`**: `range = ToRange(diagnostic.Range, model)`; `new MarkerData(ToSeverity(diagnostic.Severity), diagnostic.Message, range.StartLineNumber, range.StartColumn, range.EndLineNumber, range.EndColumn, Code: CodeToString(diagnostic.Code), Source: diagnostic.Source, ModelVersionId: documentVersion, Tags: ToMarkerTags(diagnostic.Tags))`.
  `CodeToString(JsonElement?)`: a JSON string → the string; a JSON number → its text as written when it is an integer (`GetRawText()`), else the invariant `double` text; anything else → `null`. `ToMarkerTags(int[]?)`: `1` → `MarkerTag.Unnecessary`, `2` → `MarkerTag.Deprecated`, others dropped; null / no valid tag → `null`.
  A message that is empty after `ToMarker` is the caller's problem (`MarkerService.Change` drops it).
- **Kind** (`CompletionItemKind ToCompletionItemKind(int? lspKind)`), LSP value → Core kind (the Core enum is Monaco's, P8-01): 1 Text → `Text`, 2 → `Method`, 3 → `Function`, 4 → `Constructor`, 5 → `Field`, 6 → `Variable`, 7 → `Class`, 8 → `Interface`, 9 → `Module`, 10 → `Property`, 11 → `Unit`, 12 → `Value`, 13 → `Enum`, 14 → `Keyword`, 15 → `Snippet`, 16 → `Color`,
  17 → `File`, 18 → `Reference`, 19 → `Folder`, 20 → `EnumMember`, 21 → `Constant`, 22 → `Struct`, 23 → `Event`, 24 → `Operator`, 25 → `TypeParameter`; `null` or out of range → `Text`.
- **`CompletionItem ToCompletionItem(LspCompletionItem item)`** (**no** model and no position: the default range is filled in later by `SuggestItems`, P8-14):
  - `Label = item.Label`; `LabelDetail = item.LabelDetails?.Detail`; `LabelDescription = item.LabelDetails?.Description`; `Kind = ToCompletionItemKind(item.Kind)`; `Detail = item.Detail`; `SortText = item.SortText`; `FilterText = item.FilterText`; `Preselect = item.Preselect == true`; `ProviderData = item` (the raw item, for `completionItem/resolve`).
  - **Text and range**: `(insert, replace, newText) = LspUnions.ParseTextEdit(item.TextEdit)`. If `newText` is not null: `InsertText = newText`, `Range = new CompletionRange(ToRangeUnvalidated(insert), ToRangeUnvalidated(replace))`; else `InsertText = item.InsertText ?? item.Label`, `Range = null`.
    `insertTextFormat == 2` (Snippet): the resulting `InsertText` goes through `LspSnippet.ToPlainText` (the client declared `snippetSupport = false`; a server may still send snippets).
  - **Tags**: `Deprecated` when `item.Tags` contains `1` or `item.Deprecated == true`; else `null`.
  - **Documentation**: `text = LspUnions.ParseDocumentation(item.Documentation, out bool isMarkdown)`: null → `null`; markdown → `new MarkdownString(text)`; plain → `MarkdownString.FromText(text)`; an empty text → `null`.
  - **Commit characters**: `item.CommitCharacters` → `char[]` with the **first character of each non-empty string**; null / empty → `null`.
  - **Additional edits**: `item.AdditionalTextEdits` → `TextEdit[]` (`new TextEdit(ToRangeUnvalidated(edit.Range), edit.NewText)`; the model validates them when they are applied); null or empty → `null`.
  `CompletionItem ApplyResolved(LspCompletionItem resolved)`: returns a **new** `CompletionItem` converted from `resolved` with `ToCompletionItem` and `ProviderData = resolved` (the caller gives it to `SuggestItem.CompleteResolve`, which merges only the resolvable members, P8-01).
- **`CompletionList ToCompletionList(JsonElement? result)`**: `items = LspUnions.ParseCompletionResult(result, out bool incomplete)` → `new CompletionList([.. items.Select(ToCompletionItem)], incomplete)`.
- **Trigger kind** (`int ToLspTriggerKind(CompletionTriggerKind kind)`): `Invoke` → `1`, `TriggerCharacter` → `2`, `TriggerForIncompleteCompletions` → `3`. `CompletionContextDto? ToLspContext(CompletionContext context)`: `new CompletionContextDto { TriggerKind = …, TriggerCharacter = context.TriggerCharacter }`.
- **Hover** (`Hover? ToHover(JsonElement? result)`): `null` / JSON null / not an object with `contents` → `null`; `parts = LspUnions.ParseHoverContents(contents)` → `MarkdownString`s (`IsMarkdown` → `new MarkdownString(value)`; plain → `MarkdownString.FromText(value)`); no parts → `null`; `Range = range is null ? null : ToRangeUnvalidated(range)` (an invalid `range` object → `null`); returns `new Hover(contents, range)`.
- **Locations** (`IReadOnlyList<LocationLink> ToLocationLinks(JsonElement? result)`): `links = LspUnions.ParseDefinitionResult(result)`; each → `new LocationLink(Uri: link.TargetUri, Range: ToRangeUnvalidated(link.TargetRange), OriginSelectionRange: origin is null ? null : ToRangeUnvalidated(origin), TargetSelectionRange: ToRangeUnvalidated(link.TargetSelectionRange))`. `TargetSelectionRange` equal to `TargetRange` (the `Location` case) stays set (the consumer prefers it).
- **`LspSnippet.ToPlainText(string snippet)`** (`internal static`): the LSP snippet grammar reduced to what a plain-text client needs: `\$`, `\}`, `\\` → the escaped character; `$0` (final tab stop) → empty; `$N` / `${N}` (tab stop) → empty; `${N:default}` → the default **processed recursively** (so `${1:a${2:b}}` → `ab`); `${N|one,two,three|}` → the first choice; `$NAME` / `${NAME}` (variable) → empty; `${NAME:default}` → the default;
  an unterminated `${` or a `$` not followed by a digit, letter, `_` or `{` is literal text. Never throws.
- Header: none (nothing ported).

## Tests

- [x] New (`LspConvertDiagnosticsTests`): `Severity_AllValues` (1–4, null, 0, 99), `Marker_Range_ToOneBased` (LSP `(0,2)-(0,5)` → columns 3–6), `Marker_RangeBeyondText_Clamped` (a model with one short line), `Marker_Code_String`, `Marker_Code_Integer` (`2` → `"2"`), `Marker_Code_Other_Null` (an object / array / bool), `Marker_Source_Message_Version`, `Marker_Tags`, `Marker_SwappedRange_Normalized`
- [x] New (`LspConvertCompletionTests`): `Kind_AllLspKinds` (a `[Theory]` over 1–25 with the expected Core kinds above), `Kind_NullAndUnknown_Text`, `Item_Basic` (label only: `InsertText == Label`, `Range == null`), `Item_InsertText`, `Item_TextEdit_SingleRange` (insert = replace), `Item_TextEdit_InsertReplace`, `Item_TextEdit_RangeNotValidated` (a range beyond any text stays as given, one-based),
      `Item_SnippetFormat_ConvertedToPlainText`, `Item_Tags_DeprecatedByTagOrFlag`, `Item_Documentation_Markdown`, `Item_Documentation_PlainText_Escaped`, `Item_Documentation_Empty_Null`, `Item_CommitCharacters_FirstCharOfEach`, `Item_AdditionalTextEdits_Converted`, `Item_LabelDetails`, `Item_Preselect_SortText_FilterText_Detail`, `Item_ProviderData_IsTheRawItem`,
      `List_Array_NotIncomplete`, `List_Object_Incomplete`, `List_Null_Empty`, `NoModelParameters_ConversionsRunOffTheUiThread` (call `ToCompletionList`, `ToHover` and `ToLocationLinks` from `Task.Run` with no model in the process: they must work), `ApplyResolved_CarriesResolvedMembers`, `TriggerKind_Mapping`, `Context_Mapping`
- [x] New (`LspConvertHoverTests`): `Hover_Null`, `Hover_MarkupContent_Markdown`, `Hover_MarkupContent_PlainText_Escaped`, `Hover_MarkedStringArray_Mixed`, `Hover_FencedCode_FromLanguageValue`, `Hover_EmptyContents_Null`, `Hover_Range_Converted_NotValidated`, `Hover_InvalidRange_NullRange`
- [x] New (`LspConvertLocationsTests`): `Links_Location_ToLink`, `Links_LocationLinkArray`, `Links_RangesNotValidated` (a position far beyond any text stays), `Links_Origin_Converted`, `Links_Empty_Null`, `Links_ReversedRange_Swapped`
- [x] New (`LspSnippetTests`): `Plain_Unchanged`, `FinalTabStop_Removed`, `TabStops_Removed`, `Placeholder_Default_Kept`, `Placeholder_Nested`, `Choice_FirstOption`, `Variable_Removed`, `VariableWithDefault_Default`, `Escapes`, `UnterminatedBrace_Literal`, `LoneDollar_Literal`, `Empty_Empty`, `RealisticMethodSnippet` (`Console.WriteLine(${1:value})$0` → `Console.WriteLine(value)`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Doing anything with the converted values (P8-32 …), the reverse mappings other than trigger kind and context, related information, code descriptions, snippets as real snippets.

## Suggested commit message

`feat(lsp): add the LSP to editor type conversions`
