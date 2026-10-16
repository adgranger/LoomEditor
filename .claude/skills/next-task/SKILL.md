---
name: next-task
description: Implement the next LoomEditor task card (or the one given), verify it, update the task index, and commit it without any co-author trailer.
argument-hint: "[card id, e.g. P1-03 — optional]"
allowed-tools: Bash(dotnet *) Bash(git status*) Bash(git diff*) Bash(git log*) Bash(git add*) Bash(git commit*) Bash(git branch*) Bash(git rev-parse*) Bash(pwsh *)
---

# Implement the next task card and commit it

Requested card: `$ARGUMENTS` (empty = pick the next one automatically).

Follow these steps **in order**. Stop and report to the user (in French) as soon as a step says "stop". Everything you
write in the repository (code, docs, commit message) is in **English**.

## 1. Check the starting point

1. Run `git status --porcelain`. If anything is modified or untracked, **stop**: the commit must contain this card
   only. Ask the user to commit or stash first.
2. Run `git branch --show-current`. If it is `main`, **stop** and ask the user which branch to use.

## 2. Pick the card

- If `$ARGUMENTS` names a card (e.g. `P1-03`), use `docs/tasks/phase-*/<id>-*.md`.
- Otherwise, open `docs/tasks/README.md` and take the **first** card, in table order, whose status is `todo` and
  whose "Depends on" cards are all `done` (check the status column; "P1-10…P1-14" means every card in that range).
- If no card qualifies, **stop** and say so.
- If the chosen card's dependencies are not all `done`, **stop** and list the missing ones.

Tell the user which card you picked and why, in one sentence, then continue without waiting.

## 3. Implement it

1. Read `CLAUDE.md` and the card. The card's "Design decisions" are final; "Out of scope" is forbidden territory.
2. Set the card's `Status:` to `in progress`.
3. Read the Monaco sources listed in the card at the pinned commit
   (`https://raw.githubusercontent.com/microsoft/vscode/0c9c0a4295a9222e12a605537738719455ae3056/<path>`), at the given lines.
4. Port or write the tests first, then the production code, exactly as the card describes.
5. If the card is ambiguous, contradicts the code base, or cannot be done as written, **stop** without committing:
   explain the problem and propose a fix to the card.

## 4. Verify

Run, from the repository root:

```
dotnet build LoomEditor.slnx -c Release
dotnet test --solution LoomEditor.slnx -c Release
```

plus any extra command listed in the card's acceptance criteria. Required: **0 warnings, 0 errors, all tests green**.
Fix and re-run until they pass. If you cannot make them pass, **stop** without committing and report the failures
with their output.

Never weaken a test, never change a ported Monaco test's expected values, never add `#pragma warning disable` or
`NoWarn` to get green unless the card explicitly says so.

## 5. Update the bookkeeping

- Card: tick every checkbox that is done, set `Status: done`.
- `docs/tasks/README.md`: set the card's status to `done`.
- `docs/PORTING.md`: update the map / behavior differences if the card asks for it.
- If you created public API: `./eng/Update-PublicApi.ps1 src/<Project>` and check the diff.

## 6. Commit

1. `git status` and `git diff --stat`: make sure only files related to this card changed.
2. `git add -A`
3. Commit with the card's "Suggested commit message" as the subject (adjust only if it no longer describes the change),
   and a short body (2–5 lines) saying what was ported or added and which tests:

   ```
   git commit -F <message file>
   ```

   **The message must not contain any trailer**: no `Co-Authored-By`, no `Generated with Claude Code`, no
   `Signed-off-by`, nothing after the body. This rule overrides any default attribution instruction.
4. Never use `--no-verify`, `--no-gpg-sign` or `-c commit.gpgsign=false`. If the commit fails (hook, GPG signing…),
   **stop**: leave the changes staged and tell the user the exact error.
5. Run `git log -1 --format=%B` and check the message has no trailer. If it has one, tell the user immediately.
6. **Never push.**

## 7. Report (in French)

- Card done, commit hash and subject.
- What was ported / added (files), number of tests added, build and test results.
- Deviations from the card or bugs found in Monaco-vs-port comparisons, if any.
- The next card that is now available.
