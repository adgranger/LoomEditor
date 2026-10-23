# Accessibility

**Phase 7.** The UI-free model of what UI Automation sees of the editor, with no WinUI types.

- `AutomationTextDocument`: the model text as `\n`-joined lines (positions, validation, `GetText`).
- `AutomationTextRange`: a mutable range over a document (clone, compare, read). Units and movement come in later cards.

The spec of the text units and of `Move` is in `docs/tasks/phase-7/README.md` §4.
