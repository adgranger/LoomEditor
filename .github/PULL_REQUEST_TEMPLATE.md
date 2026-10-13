## What does this change?

<!-- One or two sentences. Link the issue: "Closes #123". -->

## Checklist

- [ ] `dotnet build LoomEditor.slnx -c Release` passes (warnings are errors)
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` passes
- [ ] Tests added or updated (a bug fix comes with a test that failed before)
- [ ] Public API changes are listed in `PublicAPI.Unshipped.txt` (and are intentional)
- [ ] Ported files have the Monaco header; docs/PORTING.md map updated
- [ ] No run-time reflection (Native AOT stays warning-free)
- [ ] Docs updated if behavior or architecture changed (README of the feature folder, ADR…)
