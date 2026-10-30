# ADR 0015 — A public API for host applications: a facade, not a blanket `public`

- **Status:** accepted
- **Date:** 2026-10-30

## Context

VIGO (a git client with blame, diff, merge conflicts and staging) tried `0.1.0-alpha.1` and found every member it needed `internal`. It had read them in the packages' XML docs,
because the C# compiler writes the XML doc of internal members too. Phase 13 ([roadmap](../roadmap/phase-13-host-api.md), [design](../tasks/phase-13/README.md)) opens the API.

## Decision

1. **A facade, not a blanket `internal` → `public`.** Each Monaco internal VIGO asked for drags a large type graph (decoration options with 25 fields, injected text, stickiness,
   ids; view zones with an untyped `Data` and a change-accessor protocol). The public surface is small DTOs plus members on `LoomEditor`:

   | Need | Public | Stays internal |
   |---|---|---|
   | language of a file | `BuiltInLanguages.GetLanguageIdByFileName` (P13-01) | `LanguageRegistry` |
   | highlights | `EditorDecoration`, `EditorDecorationOptions`, `DecorationsCollection`, `LoomEditor.CreateDecorationsCollection` (P13-02, P13-04) | `ModelDecorationOptions`, `ModelDeltaDecoration`, `EditorDecorationsCollection` (the internal factory is renamed `CreateModelDecorationsCollection`) |
   | own styles | `CustomDecorationStyle`, `CustomDecorationStyles` (P13-03) | `DecorationStyle`, `DecorationStyles` |
   | column beside the editor | `ScrollChanged`, `LayoutChanged`, `LineHeight`, `GetTopForLineNumber`, `LineNumbersEnabled`, `ResetText` (P13-05, P13-06) | `HostScrollEventArgs`, `HideLineNumbers` (deleted) |
   | hidden lines, staging | `SetHiddenAreas(IReadOnlyList<Range>)`, `GutterClicked` (P13-07) | the `(ranges, source)` overload |
   | content between lines | `AddViewZone`, `RemoveViewZone` (P13-08) | `ViewZone`, `ViewZones`, the zone pointer event |

2. **Styles are global**, like CSS (copy-on-write table, thread-safe); two editors cannot draw one class name differently.
3. **No new member on `LoomDiffEditor`**: its sides are public `LoomEditor`s. Host zones and hidden areas are documented as unsupported on a side.
4. Public types of the phase are not ports: no Monaco header, XML docs name the Monaco member where one exists.

## Consequences

- A host can highlight, draw a synced column, hide lines, react to gutter clicks and put WinUI content in a gap. It cannot register languages, add hover messages, injected text or
  glyph icons, nor use per-editor styles.
- Public API of `0.1.0-alpha.1` is unchanged: the renamed members were internal (only additions in the `PublicAPI.Unshipped.txt` files).
- **Open risk:** the XML docs of the packages still list internal members; stripping them is a packaging follow-up.
- Findings that changed card decisions: `SetValue` removes every decoration (as Monaco `setValue`), so `CreateDecorationsCollection`'s doc says hosts set them again (P13-06);
  the diff editor counts other zones of a side as extra height, so a host zone there is handled like any zone but unsupported (P13-08); `MouseTarget.Position` is a view
  position, converted to a model line for `GutterClicked` (P13-07).
- Proof: the Native AOT self-check (5 of 5) and the package test app, which calls every new member (P13-10).
