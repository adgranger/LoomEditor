# Releasing LoomEditor

Steps prefixed **You:** can only be done by the repository owner (accounts, secrets, settings, the tag).

## 1. How a release works

1. You push a tag `vX.Y.Z[-pre]` (first release: `v0.1.0-alpha.1`); the `Release` workflow
   ([release.yml](../.github/workflows/release.yml)) starts.
2. Job "Gates and packages" runs every gate and stops at the first failure: tag format, tag commit reachable from
   `main`, CHANGELOG section present, Release build with 0 warnings, all tests, `eng/Test-Packages.ps1` (pack, content
   check, Native AOT consumer app).
3. Job "Publish to nuget.org and GitHub" waits for your approval of the `nuget` environment.
4. It logs in to nuget.org with Trusted Publishing (`NuGet/login@v1.2.0`, a temporary key from the GitHub OIDC token;
   only this job has `id-token: write`) and pushes `LoomEditor.Core`, `.TextMate`, `.Themes`, `.View`, `.Rendering`, `.WinUI` (in that order) with their symbols (`.snupkg`).
5. It creates the GitHub Release with the same files and the CHANGELOG notes.

The version of the packages is the tag. `VersionPrefix`/`VersionSuffix` in `Directory.Build.props` are for local builds
only; nothing is edited at release time.

## 2. One-time setup

nuget.org:

- [ ] **You:** create an account with 2FA enabled.
- [ ] **You:** check that the six ids are free: `https://www.nuget.org/packages/<id>` must return 404 for
  `LoomEditor.Core`, `LoomEditor.TextMate`, `LoomEditor.Themes`, `LoomEditor.View`, `LoomEditor.Rendering`,
  `LoomEditor.WinUI`. If one is taken, stop and
  choose another id before tagging (ids are in the csproj files, `eng/Pack.ps1`, `eng/Test-Packages.ps1`, the workflow
  and the package READMEs).
- [ ] **You (optional):** request the ID prefix reservation `LoomEditor.` (nuget.org → account menu →
  "Contact support" / "Reserve ID prefix" form). It takes days and gives the verified check mark; your first push owns
  the ids anyway.
- [ ] **You:** create the Trusted Publishing policy (account menu → "Trusted Publishing" → add a policy), owner = your
  account, repository owner `adgranger`, repository `LoomEditor`, workflow file `release.yml` (file name only, no
  path), environment `nuget`, scopes: "Push" with "Push new packages and package versions" (the form has no glob pattern; "Unlist or
  relist" can stay unchecked; values are case-insensitive). There is no API key: the workflow trades a short-lived GitHub OIDC token for a temporary key
  (valid one hour) right before the push ([docs](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)).
- [ ] **You:** if the repository is private, the policy is only *temporarily active* for 7 days until the first
  successful publish (nuget.org needs the repository ids from the first token); restart the window in the policy list
  if it expires before the tag.

GitHub:

- [ ] **You:** Settings → Environments → `nuget`: required reviewer = you; deployment branches/tags limited to `v*`.
- [ ] **You:** add the secret `NUGET_USER` **to that environment**: your nuget.org profile name (the user name, not
  your e-mail address). It is the only value the workflow needs; there is no key to store or rotate.
- [ ] **You:** Settings → Rules → Rulesets: a tag ruleset on `v*` (restrict creation to admins, block deletion and
  force-push).
- [ ] **You:** a branch ruleset on `main` requiring the three CI checks "Build and test", "Native AOT publish (win-x64)",
  and "Native AOT publish (win-arm64)", and a linear history (signed commits optional). Do not require "Pack NuGet
  packages": that job runs only on pushes to `main` and on demand, so a pull request would wait for it forever.
- [ ] **You:** Settings → Actions → General: default workflow permissions "Read repository contents".

## 3. Before the tag

- [ ] [docs/release-checklist.md](release-checklist.md) is done.
- [ ] `main` is green.
- [ ] In `CHANGELOG.md`, rename `## [Unreleased]` to `## [<version>] - <YYYY-MM-DD>` and add a new empty
  `## [Unreleased]` above it; commit it on a branch and merge it through a pull request.
- [ ] Local gates:

```powershell
./eng/Invoke-ReleaseBuild.ps1 -Version 0.1.0-alpha.1
```

- [ ] **You:** dry run on GitHub: Actions → Release → Run workflow → enter the version. It must be green and the
  `nuget-packages` artifact must hold twelve files (six `.nupkg`, six `.snupkg`). Nothing is published.

## 4. Cut the release

**You:**

```bash
git switch main && git pull
```

```bash
git tag -s v0.1.0-alpha.1 -m "v0.1.0-alpha.1"
```

(a plain `git tag -a` also works)

```bash
git push origin v0.1.0-alpha.1
```

Open the run in the Actions tab, approve the `nuget` environment, and wait for green.

Never move or reuse a tag that was pushed: a published version is immutable.

## 5. After the push

nuget.org validates and indexes the packages (a few minutes up to an hour; the page says "validating" first, and the
symbols appear on the package page).

- [ ] **You:** check the six package pages: README rendering, license, repository link, "Symbols".
- [ ] **You:** install check in a scratch folder:

```bash
dotnet new console && dotnet add package LoomEditor.Core --prerelease && dotnet restore
```

- [ ] **You:** check the GitHub Release: six `.nupkg`, six `.snupkg`, notes.
- [ ] Optional: add the nuget badges to `README.md`, and set `VersionPrefix`/`VersionSuffix` of local builds to the
  next version.

## 6. Rolling back

| Case | What to do |
|---|---|
| (a) A gate failed before the push | Fix, delete the remote tag (`git push origin :refs/tags/v<version>`) and the local one (`git tag -d v<version>`), tag again. Allowed because nothing was published. |
| (b) The publish job failed halfway | "Re-run failed jobs" (`--skip-duplicate` makes it safe). |
| (c) A bad version reached nuget.org | It cannot be deleted. **Unlist** it: nuget.org → Manage Packages → the version → "List in search results" off, or `dotnet nuget delete <id> <version> --source https://api.nuget.org/v3/index.json --api-key <key>` (which only unlists on nuget.org; the key is one created by hand for this, since the CI key is temporary, or use the web UI). Mark the GitHub Release as pre-release or edit its notes, then publish the fix as the next version (`alpha.2`, `0.1.1`…). Unlist all six packages of the version together. |
| (d) The publish path is compromised | Delete the Trusted Publishing policy on nuget.org (no long-lived secret exists to rotate), then fix the workflow or the environment rules and create the policy again. |

## 7. Versions and public API

- Order: `alpha.N` → `beta.N` → `rc.N` → stable. One version for the six packages.
- The `PublicAPI.Unshipped.txt` files are moved into `PublicAPI.Shipped.txt` when the first **stable** version is cut
  (not for alphas).
- A package id never changes.

## 8. Later improvements

- Package icon: `PackageIcon` plus a 128×128 PNG in `Directory.Build.props`, and one more check in `eng/Pack.ps1`.
- Package signing.
- `LoomEditor.Lsp` (not packable yet, `IsPackable=false`): when it ships, set `IsPackable` to true and add its id to the
  publish order in `release.yml`, to `eng/Pack.ps1` (projects, dependency table, file count) and to this document.

## 9. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| 403 on push | The policy scopes do not cover the id, or the policy is inactive (owner left the organization, 7-day window expired): fix the policy (section 2). |
| "NuGet login" step fails (token exchange refused) | The policy does not match the token: repository owner/name, workflow file name `release.yml` (no path), environment `nuget`, or the policy is inactive. Also check `NUGET_USER` is the profile name, not an e-mail. |
| "Unable to get ACTIONS_ID_TOKEN_REQUEST_URL" | The job lacks `permissions: id-token: write` (only the publish job has it). |
| Push fails with "key expired" | The temporary key lasts one hour: the login step must stay right before the push; re-run the failed job. |
| 409 on push | The version already exists; harmless with `--skip-duplicate`. |
| "points to a commit that is not on main" | The tag was created on another branch: delete it (case (a)) and tag `main`. |
| "CHANGELOG.md has no (or an empty) section [x.y.z]" | Rename `[Unreleased]` first (section 3). |
| "The secret NUGET_USER is not set in the environment "nuget"" | Add it to the environment, not to the repository (section 2). |
| "validation failed" mail from nuget.org | Read the mail. Missing README or license fields would already have failed `eng/Pack.ps1`. |
| AOT test fails only on the runner | Re-run once; if it persists, run `./eng/Test-Packages.ps1` locally. |

## 10. To verify at the first release

The Trusted Publishing documentation does not say whether a policy can create a package id that does not exist yet on
nuget.org (the six ids are new) beyond the scope "publish new packages". If the first push of an id is refused with
403 although the policy matches, publish that first version once by hand with a temporary API key (scoped to
`LoomEditor.*`, short expiry, deleted afterwards), then let the workflow do the next ones. Also verify at that
release that the policy leaves the "temporarily active" state after the first successful publish.
