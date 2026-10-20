# Phase 4 — TextMate grammars + VS Code themes

**Done when:** golden tests are green for 10 languages (C#, TypeScript, JavaScript, JSON, HTML, CSS, XML, Markdown,
Python, YAML). ✅ Done on 2026-10-20: 30 golden tests (scopes and Dark Modern / Light Modern colors) are green; see `docs/PORTING.md`, "Done in phase 4".

Step-by-step task cards: [docs/tasks/README.md](../tasks/README.md) (P4-01 … P4-50). Design:
[ADR 0007](../adr/0007-textmate-regex-engine.md) (regex engine) and [ADR 0008](../adr/0008-syntax-highlighting.md).

- [x] ADR: TextMate regex engine — [ADR 0007](../adr/0007-textmate-regex-engine.md): native Oniguruma 6.9.10, frozen; P4-01 … P4-04
- [x] Port `vscode-textmate` (grammar loading without reflection, tokenization, rule stack) — P4-05 … P4-22
- [x] Monaco's tokens in the text model — P4-23 … P4-32
- [x] Background, incremental, cancellable tokenization; results published on the UI thread — P4-33 … P4-38
- [x] VS Code theme loading (`.json`, `include`, token colors) — P4-39 … P4-41
- [x] Highlighting in the control (`Language`, `Theme`) and the Gallery — P4-42 … P4-46
- [x] Golden tests + regeneration script; 10 languages — P4-47, P4-48
- [x] Benchmarks and close — P4-49, P4-50
