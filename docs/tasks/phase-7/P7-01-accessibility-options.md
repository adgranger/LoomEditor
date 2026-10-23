# P7-01 — Accessibility options and the effective wrapping indent

- **Status:** done
- **Depends on:** P6-63
- **Size:** S

## Goal

The view layer knows whether the editor is in screen reader mode (Monaco's `accessibilitySupport`), and the wrapping indent is
forced to `None` in that mode, so that the wrapped lines match what a screen reader reads.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/platform/accessibility/common/accessibility.ts` | 36–45 | `AccessibilitySupport` (`Unknown = 0`, `Disabled = 1`, `Enabled = 2`) |
| `src/vs/editor/common/config/editorOptions.ts` | 1475–1512 | `EditorAccessibilitySupport.compute`: an `Unknown` option takes the environment's value |
| `src/vs/editor/common/config/editorOptions.ts` | 3090–3105 | `EditorWrappingIndent`-style `compute`: `Enabled` → `WrappingIndent.None` |

## Files to create or change

- `src/LoomEditor.View/Layout/AccessibilitySupport.cs` — create (the enum; header comment names the Monaco file)
- `src/LoomEditor.View/Layout/EditorAccessibilityOptions.cs` — create
- `src/LoomEditor.View/Layout/EditorLayoutOptions.cs` — change: property `Accessibility` and method `GetEffectiveWrapping()`
- `src/LoomEditor.View/Layout/ViewModel.cs` — change: line 57 reads `layoutOptions.GetEffectiveWrapping()`
- `src/LoomEditor.View/Layout/ViewModel.Wrapping.cs` — change: line 13 reads `_layoutOptions.GetEffectiveWrapping()`
- `tests/LoomEditor.View.Tests/TestHelpers/TestCodeEditor.cs` — change: `TestCodeEditorOptions` gets `EditorAccessibilityOptions? Accessibility = null` (last parameter) and the
  `EditorLayoutOptions` built in the constructor gets `Accessibility = options.Accessibility ?? new()`
- `tests/LoomEditor.View.Tests/Layout/EditorAccessibilityOptionsTests.cs` — create
- `tests/LoomEditor.View.Tests/Layout/ViewModelAccessibilityWrappingTests.cs` — create

## Design decisions (already made — do not change)

- `EditorAccessibilityOptions` is a `sealed record` with `AccessibilitySupport Setting { get; init; } = Unknown` (the option value, Monaco `'auto'` = `Unknown`),
  `AccessibilitySupport Detected { get; init; } = Unknown` (the environment: Monaco `env.accessibilitySupport`),
  `AccessibilitySupport Effective => Setting == Unknown ? Detected : Setting`, and `bool IsEnabled => Effective == Enabled`.
- `EditorLayoutOptions.GetEffectiveWrapping()` returns `Wrapping` unchanged unless `Accessibility.IsEnabled`, in which case it returns
  `Wrapping with { WrappingIndent = WrappingIndent.None }`. Nothing else is overridden here (forced viewport wrapping is P7-03; `wrappingStrategy: advanced`
  does not exist in LoomEditor, see the XML doc of `EditorWrappingOptions`).
- Only the two `ViewModel` sites above read the indent: do not touch `EditorLayoutInfoComputer` (it never reads the indent).
- Update the "Not ported" remark of `EditorWrappingOptions` so that it no longer lists the accessibility overrides as missing (`wrappingStrategy` stays not ported).
- All types `internal`; XML doc and the `Threading` remark on every class; ported file header on `AccessibilitySupport.cs` only.

## Tests

- [x] New: `EffectiveSupport_SettingUnknown_UsesDetected`, `EffectiveSupport_SettingDisabled_IgnoresDetected`, `EffectiveSupport_SettingEnabled_IgnoresDetected`,
      `IsEnabled_OnlyWhenEffectiveIsEnabled` (a theory over the 9 combinations of `Setting` × `Detected`)
- [x] New: `GetEffectiveWrapping_Disabled_ReturnsTheWrappingUnchanged` (same instance), `GetEffectiveWrapping_Enabled_ForcesIndentNone`,
      `GetEffectiveWrapping_Enabled_KeepsEveryOtherSetting`
- [x] New (`ViewModelAccessibilityWrappingTests`): with text `"    aaaa bbbb cccc"`, `WordWrap.WordWrapColumn`, `WordWrapColumn = 10`, `WrappingIndent.Same`:
      `Wrapped_SettingDisabled_ContinuationLinesKeepTheIndent` (view lines `"    aaaa "`, `"    bbbb "`, `"    cccc"`) and
      `Wrapped_SettingEnabled_ContinuationLinesHaveNoIndent` (view lines `"    aaaa "`, `"bbbb cccc"`). If the break positions of the line-break computer differ by one
      column, change `WordWrapColumn` in the test, never the production code; keep the shape of the assertions (indent kept / indent dropped).
- [x] New: `SetOptions_TogglingAccessibility_RewrapsTheLines` (start with `Disabled`, call `viewModel.SetOptions(viewModel.LayoutOptions with { Accessibility = … Enabled }, viewModel.ViewLayoutOptions)`,
      the view line count goes from 3 to 2)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: rows for `AccessibilitySupport` and `EditorAccessibilityOptions`; the "Accessibility-driven wrapping" row of "Cut points for later phases" is marked "wrapping indent: P7-01, long lines: P7-03"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Forced viewport wrapping for long lines (P7-02, P7-03), the control property and the screen reader detection (P7-04).

## Suggested commit message

`feat(view): add the accessibility options and the screen reader wrapping indent`
