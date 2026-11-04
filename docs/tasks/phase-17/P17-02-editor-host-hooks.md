# P17-02 — Share the framework-free members of `LoomEditor.HostHooks.Host.cs`

- **Status:** done
- **Depends on:** P17-01
- **Size:** S

**Recommended model:** Sonnet

## Goal

The scroll and layout hooks of `LoomEditor` that name no framework type exist once, in the shared `LoomEditor.HostHooks.cs`.

## Sources

Not a Monaco port. Read first: [phase-17/README.md](README.md) (rules). `src/LoomEditor.HostShared/Controls/LoomEditor.HostHooks.cs` (the shared part P16-04 started).

## Duplicated block (SonarCloud, PR #26, 2026-11-03)

| Avalonia | WinUI | Members |
|---|---|---|
| `src/LoomEditor.Avalonia/Controls/LoomEditor.HostHooks.Host.cs` 38+33 | `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.Host.cs` 44+33 | `GetWhitespaces`, `GetTopForLineNumber`, `ResetText` (and the members around them, which are copies too) |

None of these members names a framework type: both `.Host.cs` files are almost entirely framework-free.

## Files to create or change

- `src/LoomEditor.HostShared/Controls/LoomEditor.HostHooks.cs` — add, from the WinUI host file (same order, same XML docs): `ScrollChanged`, `LayoutChanged`,
  `ViewZones`, `CoordinatesConverter`, `IsWrapping`, `LayoutInfo`, `GetWhitespaces`, `GetTopForLineNumber`, `ResetText`, `RaiseScrollChanged`,
  `RaiseContentSizeChanged`. Add the unconditional `using LoomEditor.Core.Text;` (for `Position`).
- `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.Host.cs` — keeps only what Avalonia does not have: `_overviewRulerEnabled`, `GetWhitespaceViewportData`,
  `InvalidateCanvas`, `CreateModelDecorationsCollection`, `OverviewRulerEnabled`, `ApplyHostLayoutOptions`. Remove the usings that become unused.
- `src/LoomEditor.Avalonia/Controls/LoomEditor.HostHooks.Host.cs` — keeps only `InvalidateCanvas` (and the stray `/// <remarks>` above the class goes).
- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — delete the `LoomEditor.HostHooks.Host.cs` pair from `KnownDuplicates`.

## Design decisions (already made — do not change)

- Shared file first, WinUI wording: the one exception is the `<param name="lineNumber">` text of `GetTopForLineNumber`, where the WinUI file has a broken
  character (`1 � line count`): use the Avalonia wording ("clamped to `1` to the line count").
- The WinUI-only members stay WinUI-only (no new Avalonia member: no behavior change).
- `HostHooks.cs` stays under ~400 lines; it has room.

## Tests

- [x] No new test. The Avalonia headless tests stay green **unchanged**.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green, the duplication test included
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] The pair is gone from `KnownDuplicates`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Giving Avalonia the WinUI-only members.

## Suggested commit message

`refactor(hosts): share the scroll and layout hooks of LoomEditor`
