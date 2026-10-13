# P<n>-<nn> — <Short title>

- **Status:** todo | in progress | done
- **Depends on:** P<n>-<nn>, …
- **Size:** S (< 150 lines of production code) | M (150–400) | L (split it!)

## Goal

One or two sentences: what exists after this task that did not exist before.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/...` | 10–80 | `functionName`, `ClassName.method` |

## Files to create or change

- `src/LoomEditor.Core/...` — create
- `tests/LoomEditor.Core.Tests/...` — create

## Design decisions (already made — do not change)

- …

## Tests

- [ ] Port: `suite name` › `test name`, …
- [ ] New: …

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (if a file was ported)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

- …

## Suggested commit message

`feat(core): …`
