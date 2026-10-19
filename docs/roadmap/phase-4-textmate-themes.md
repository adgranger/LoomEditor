# Phase 4 — TextMate grammars + VS Code themes

**Done when:** golden tests are green for 10 languages (C#, TypeScript, JavaScript, JSON, HTML, CSS, XML, Markdown,
Python, YAML).

Step-by-step task cards: [docs/tasks/README.md](../tasks/README.md) (P4-01 … P4-50). Design:
[ADR 0007](../adr/0007-textmate-regex-engine.md) (regex engine) and [ADR 0008](../adr/0008-syntax-highlighting.md).

- [ ] ADR: TextMate regex engine — [ADR 0007](../adr/0007-textmate-regex-engine.md): native Oniguruma 6.9.10, frozen; P4-01 … P4-04
- [ ] Port `vscode-textmate` (grammar loading without reflection, tokenization, rule stack) — P4-05 … P4-22
- [ ] Monaco's tokens in the text model — P4-23 … P4-32
- [ ] Background, incremental, cancellable tokenization; results published on the UI thread — P4-33 … P4-38
- [ ] VS Code theme loading (`.json`, `include`, token colors) — P4-39 … P4-41
- [ ] Highlighting in the control (`Language`, `Theme`) and the Gallery — P4-42 … P4-46
- [ ] Golden tests + regeneration script; 10 languages — P4-47, P4-48
- [ ] Benchmarks and close — P4-49, P4-50
