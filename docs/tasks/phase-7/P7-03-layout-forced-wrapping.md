# P7-03 — Layout: force viewport wrapping for long-line models in screen reader mode

- **Status:** todo
- **Depends on:** P7-02
- **Size:** S

## Goal

In screen reader mode, a model dominated by very long lines wraps at the viewport width whatever the `WordWrap` setting is (Monaco: the
"minified" word wrap), and the view model can be told whether the model is dominated by long lines.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/config/editorOptions.ts` | 2975–2992 | the `if (accessibilitySupport === Enabled && wordWrapOverride1 === 'inherit' && isDominatedByLongLines)` branch of `EditorLayoutInfoComputer.computeLayout` |
| `src/vs/editor/common/config/editorOptions.ts` | 3017–3027 | `wrappingColumn` computed from the viewport when `isViewportWrapping` (already ported; check that it applies) |
| `src/vs/editor/browser/config/editorConfiguration.ts` | 181–187 | `setIsDominatedByLongLines`: stores the flag and recomputes the options |

## Files to create or change

- `src/LoomEditor.View/Layout/EditorLayoutInfoComputer.cs` — change: `EditorLayoutInfoComputerEnv` gets `bool IsDominatedByLongLines = false` (last parameter); the
  `isViewportWrapping` / `wrappingColumn` lines (74–75) follow Monaco's `if / else if` chain; update the remarks (accessibility is now ported, `isWordWrapMinified` is not)
- `src/LoomEditor.View/Layout/ViewModel.cs` — change: field `_isDominatedByLongLines`, passed to the env in `ComputeLayoutInfo` (line ~286)
- `src/LoomEditor.View/Layout/ViewModel.Wrapping.cs` — change: `public bool IsDominatedByLongLines` and `public void SetIsDominatedByLongLines(bool value)`
- `src/LoomEditor.View/Layout/EditorLayoutInfo.cs` — change: update the remark "Not ported: `isWordWrapMinified` (accessibility, phase 7)" to say it is not ported because nothing reads it
- `tests/LoomEditor.View.Tests/Layout/EditorLayoutInfoComputerAccessibilityTests.cs` — create (follow the env helper of `EditorLayoutInfoComputerTests.cs`)
- `tests/LoomEditor.View.Tests/Layout/ViewModelAccessibilityWrappingTests.cs` — change: add the view model tests below

## Design decisions (already made — do not change)

- The chain is exactly Monaco's:
  1. `options.Accessibility.IsEnabled && env.IsDominatedByLongLines` → `isViewportWrapping = true`, `wrappingColumn` stays `-1` (it is computed from the viewport below);
  2. else `WordWrap` is `On` or `Bounded` → `isViewportWrapping = true`;
  3. else `WordWrap == WordWrapColumn` → `wrappingColumn = WordWrapColumn`.
  Branch 1 wins over every setting, **including** `WordWrapColumn`. The later `if (isViewportWrapping)` block is unchanged, so `Bounded` still takes the minimum with
  `WordWrapColumn` (Monaco tests the user's `wordWrap === 'bounded'`, not the branch).
- `SetIsDominatedByLongLines(bool)` does nothing when the value is unchanged; otherwise it stores it and runs the same three calls as `SetViewportSize`:
  `RecomputeLayoutInfo(); UpdateWrappingSettings(); UpdateCursorConfiguration();`.
- The view model never computes the flag itself (Monaco: the editor widget does, once, when the model is attached): P7-04 feeds it.
- The minimap layout already receives `isViewportWrapping`; give it the new value (the variable is computed before the call).

## Tests

- [ ] New (`EditorLayoutInfoComputerAccessibilityTests`): compute `on` = layout with `WordWrap.On` (Accessibility off, not dominated) once, then:
      `ForcedWrapping_EnabledAndDominated_WrapsAtTheViewportEvenWhenWordWrapIsOff` (`IsViewportWrapping` true, `WrappingColumn == on.WrappingColumn`),
      `ForcedWrapping_EnabledButNotDominated_DoesNotWrap` (`IsViewportWrapping` false, `WrappingColumn == -1`),
      `ForcedWrapping_DisabledAndDominated_DoesNotWrap` (`Setting = Disabled`),
      `ForcedWrapping_DetectedEnabledWithAutoSetting_Wraps` (`Setting = Unknown`, `Detected = Enabled`),
      `ForcedWrapping_BeatsWordWrapColumn` (`WordWrap.WordWrapColumn`, column 50 → `WrappingColumn == on.WrappingColumn`),
      `ForcedWrapping_BoundedStillTakesTheMinimum` (`Bounded`, column 5 → `WrappingColumn == 5`)
- [ ] New (`ViewModelAccessibilityWrappingTests`): `SetIsDominatedByLongLines_InScreenReaderMode_WrapsTheLines` (text: one line of 300 `a` separated by a space every 10, viewport 100 wide,
      accessibility `Setting = Enabled`; before the call the view line count is 1, after it is greater than 1; after `SetIsDominatedByLongLines(false)` it is 1 again),
      `SetIsDominatedByLongLines_SettingDisabled_DoesNotWrap`, `SetIsDominatedByLongLines_SameValue_DoesNotRecompute` (the `LayoutInfoChanged` event count does not grow)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: "Behavior differences" row of the layout updated (the accessibility row is now "ported except `isWordWrapMinified`, which nothing reads"); the cut point "Accessibility-driven wrapping" says "done (P7-01, P7-03); control wiring P7-04"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The control property and the detection of a screen reader (P7-04), `wrappingStrategy: advanced`.

## Suggested commit message

`feat(view): wrap long-line models at the viewport in screen reader mode`
