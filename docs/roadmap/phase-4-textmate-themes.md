# Phase 4 — TextMate grammars + VS Code themes

**Done when:** golden tests are green for 10 languages.

- [ ] ADR: TextMate regex engine (Oniguruma is archived upstream — see ADR 0001)
- [ ] Port `vscode-textmate` (grammar loading with source-generated JSON, tokenization, rule stack)
- [ ] Background, incremental, cancellable tokenization; results published on the UI thread
- [ ] VS Code theme loading (`.json`, `include`, token colors)
- [ ] Golden tests + regeneration script; 10 languages
