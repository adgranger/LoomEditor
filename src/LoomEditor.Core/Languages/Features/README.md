# Language features

Public data types and provider interfaces that language features speak (Monaco `languages.ts`): completion, hover and
definition, plus `MarkdownString`. An application or the LSP client implements the providers; the registry and the
editor code that calls them come in later cards.

Files: `MarkdownString`, `CompletionItemKind`, `CompletionItemTag`, `CompletionRange`, `CompletionItem`, `CompletionList`,
`CompletionTriggerKind`, `CompletionContext`, `ICompletionItemProvider`, `Hover`, `IHoverProvider`, `Location`,
`LocationLink`, `IDefinitionProvider`, `LanguageFeatureRegistry<T>`, `LanguageFeaturesService`.
