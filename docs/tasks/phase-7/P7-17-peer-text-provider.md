# P7-17 — The UIA peer: `ITextProvider`, `IValueProvider` and the read members of the range provider (tracer bullet)

- **Status:** done
- **Depends on:** P7-16
- **Size:** L if done carelessly — keep it to what is listed (≈350 lines, mostly one-line delegations)
- **Recommended model:** Opus (tracer bullet: managed UIA providers under Native AOT / CsWinRT; stop and report on failure)

## Goal

Narrator, NVDA and Accessibility Insights see the editor as an `Edit` control with the **Text** and **Value** patterns: they can read the whole text, the selection, the visible range, ask for text of ranges,
bounding rectangles and the range nearest to a point. (Moving ranges and selecting come in P7-18; events in P7-19.) This card is also the **tracer bullet** of ADR 0010 decision 11: it proves that a managed
`ITextProvider` and `ITextRangeProvider` work under Native AOT before more is built on them.

## Sources

None to port. WinUI API reference (read the member lists before writing; do not guess signatures): `Microsoft.UI.Xaml.Automation.Provider.ITextProvider`, `ITextRangeProvider`, `IValueProvider`, `IRawElementProviderSimple`;
`Microsoft.UI.Xaml.Automation.Text.TextUnit`, `TextPatternRangeEndpoint`, `SupportedTextSelection`; `Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer`, `PatternInterface`.
The View model it delegates to: P7-05 … P7-11 (`AutomationTextDocument`, `AutomationTextRange`, `AutomationTextGeometry`).

## Files to create or change

- `src/LoomEditor.WinUI/Accessibility/LoomEditorAutomationPeer.cs` — change: implement `ITextProvider` and `IValueProvider`, `GetPatternCore`
- `src/LoomEditor.WinUI/Accessibility/LoomEditorTextRangeProvider.cs` — create (`ITextRangeProvider`, the read members; the others throw, see decisions)
- `src/LoomEditor.WinUI/Accessibility/RendererLineMetrics.cs` — create (`IAutomationLineMetrics` over the renderer)
- `src/LoomEditor.WinUI/Input/ScreenCoordinates.cs` — change: `ToElement(UIElement element, Point screenPixels)` (the inverse of `ToScreenPixels`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Accessibility.cs` — change: the internal accessors listed below
- `src/LoomEditor.WinUI/Accessibility/README.md` — change
- `docs/testing/phase-7-manual-tests.md` — change: section 3 below

## Design decisions (already made — do not change)

- **The peer** `internal sealed partial class LoomEditorAutomationPeer(LoomEditorControl owner) : FrameworkElementAutomationPeer(owner), ITextProvider, IValueProvider` (keep `partial`: CsWinRT generates the AOT marshalling for managed WinRT interface implementations).
  - `GetPatternCore(PatternInterface patternInterface)`: `Text` → `this`, `Value` → `this`, anything else → `base.GetPatternCore(patternInterface)`.
  - `ITextProvider`: `DocumentRange` → `Wrap(AutomationTextRange.ForDocument(owner.AutomationDocument))`; `SupportedTextSelection` → `SupportedTextSelection.Multiple`;
    `GetSelection()` → one provider per `owner.AutomationDocument.ViewModel.GetSelections()` entry, in that order (the primary cursor first), each over `new Range(s.StartLineNumber, s.StartColumn, s.EndLineNumber, s.EndColumn)`;
    `GetVisibleRanges()` → one provider over `owner.AutomationGeometry.GetVisibleRange()`; `RangeFromChild(IRawElementProviderSimple)` → the document range (the editor has no children);
    `RangeFromPoint(Point screenLocation)` → `ScreenCoordinates.ToElement(owner, screenLocation)` → `owner.AutomationGeometry.PositionFromPoint(x, y)` → a degenerate range there; if `ToElement` fails (no `XamlRoot`) → the degenerate range at the primary cursor.
  - `IValueProvider`: `IsReadOnly` → `owner.IsReadOnly`; `Value` → `owner.Model.GetValue(EndOfLinePreference.LF)`; `SetValue(string value)` → if `owner.IsReadOnly` throw `ElementNotEnabledException`; else `owner.Text = value ?? string.Empty`.
  - `Wrap(AutomationTextRange)` → `new LoomEditorTextRangeProvider(owner, ProviderFromPeer(this), range)`. `ProviderFromPeer` is the protected method of `AutomationPeer` that returns the `IRawElementProviderSimple` of a peer.
  - Keep `GetClassNameCore`, `GetAutomationControlTypeCore` (`Edit`) and `GetNameCore` as they are.
- **The range provider** `internal sealed partial class LoomEditorTextRangeProvider : ITextRangeProvider` holds `LoomEditorControl _owner`, `IRawElementProviderSimple _element`, `AutomationTextRange _range`. Members implemented now:
  - `Clone()` → a new provider over `_range.Clone()`; `Compare(ITextRangeProvider other)` → `other is LoomEditorTextRangeProvider p && _range.Compare(p._range)`;
    `CompareEndpoints(TextPatternRangeEndpoint e, ITextRangeProvider other, TextPatternRangeEndpoint oe)` → `_range.CompareEndpoints(...)` (cast the enums by value: they have the same integers as `AutomationTextEndpoint`; `other` not ours → `ArgumentException`);
    `GetText(int maxLength)` → `_range.GetText(maxLength)`; `GetEnclosingElement()` → `_element`; `GetChildren()` → `[]`;
    `GetBoundingRectangles()` → for each `AutomationRect r` of `_owner.AutomationGeometry.GetBoundingRectangles(_range.Range)`: `ScreenCoordinates.ToScreenPixels(_owner, new Rect(r.X, r.Y, r.Width, r.Height))` (skip if `null`), appended as `x, y, width, height` to a `double[]`;
    `GetAttributeValue(int attributeId)` and `FindAttribute(int attributeId, object value, bool backward)` as in the table below.
  - Not yet: `ExpandToEnclosingUnit`, `FindText`, `Move`, `MoveEndpointByUnit`, `MoveEndpointByRange`, `Select`, `AddToSelection`, `RemoveFromSelection`, `ScrollIntoView` → `throw new NotImplementedException("Added by P7-18.");` (the only stubs of phase 7; P7-18 removes them).
  - Every public member first runs `Guard.Assert(_owner.DispatcherQueue.HasThreadAccess)` (Debug only, `LoomEditor.Core` `Guard`): UIA calls XAML peers on the UI thread; if the assertion ever fires, record it as a risk in the card and stop.
- **Attributes** (UIA ids from `UIAutomationClient.h`): `40005` FontName → `_owner.FontFamily.Source`; `40006` FontSize → `_owner.FontSize * 0.75` (UIA wants points; `FontSize` is in DIPs); `40007` FontWeight → `400`; `40013` IsHidden → `false`;
  `40014` IsItalic → `false`; `40015` IsReadOnly → `_owner.IsReadOnly`. Every other id → `DependencyProperty.UnsetValue` (the WinRT form of UIA's "not supported"; if Accessibility Insights shows it as an error, record it as a risk).
  `FindAttribute`: for a supported id whose value `Equals(value)` → a clone of this range; otherwise `null` (the attributes are uniform over the whole document: ADR 0010 "Not in phase 7").
- **The control exposes** (in `LoomEditor.Accessibility.cs`, `internal`): `AutomationTextDocument AutomationDocument` (created once, `new AutomationTextDocument(_viewModel)`), `AutomationTextGeometry AutomationGeometry` (a new `AutomationTextGeometry(AutomationDocument, new RendererLineMetrics(this))` per call: cheap),
  `TextViewRenderer? AutomationRenderer => _renderer`. `ViewModel` stays private: the peer uses `owner.AutomationDocument.ViewModel`.
- **`RendererLineMetrics`**: `IAutomationLineMetrics` implemented over `owner.AutomationRenderer` and `owner.AutomationDocument.ViewModel`: `TryGetHorizontalRange` → `renderer.TryGetHorizontalRange(viewModel, …)` (returns `false` and zeros when the renderer is null: the canvas is not ready);
  `GetLineWidth` and `GetPositionAtCoordinate` → a lazily created `renderer.CreateLineHitTester(viewModel)`; with no renderer they return `0` / `null`.
- **`ScreenCoordinates.ToElement`**: the exact inverse of `ToScreenPixels` (screen pixels → client pixels with `ScreenToClient`-style subtraction of the same `ClientToScreen` origin → DIPs by dividing by `RasterizationScale` → element coordinates with `element.TransformToVisual(null).Inverse.TransformPoint`);
  returns `Point?` (`null` when the element is not in a window).
- No logic in the WinUI classes beyond type conversion: anything that looks like a rule goes back to the View model (a new card), not here.
- Native AOT: `LoomEditorAutomationPeer` and `LoomEditorTextRangeProvider` are `partial`; no reflection; if the build prints a CsWinRT warning (`CsWinRT1028`, `CsWinRT1029` …) about these classes, fix the cause (usually `partial`, visibility or an interface not marked), never suppress it.

## Tests

No WinUI unit test (ADR 0010 decision 2); the model is tested by P7-05 … P7-11 and the in-process self-check is P7-24. Manual checks, `docs/testing/phase-7-manual-tests.md` section 3 "UIA text pattern (read)"
(tools: Accessibility Insights for Windows → Live inspect, or `inspect.exe` from the Windows SDK; the Gallery running with a C# sample):

- [x] 3.1 Hover the editor: ControlType `Edit`, Name `Code editor`, patterns list contains **Text** and **Value** (and no error)
- [x] 3.2 Text pattern pane: `DocumentRange.GetText(-1)` equals the editor text; `SupportedTextSelection` = Multiple
- [ ] 3.3 Select some text in the editor, press "Get selection" in the tool → the selected text; with two cursors (`Ctrl+Alt+Down`) two ranges
- [ ] 3.4 `GetVisibleRanges` → the text on screen; scroll and repeat
- [ ] 3.5 `RangeFromPoint` over a word → a degenerate range at that character (expand it to Word in P7-18's check 4.x)
- [ ] 3.6 `GetBoundingRectangles` of a selection → rectangles that sit on the selected text (compare with the highlighted rectangle on screen), at 100 % and 200 % display scale
- [ ] 3.7 `IsReadOnly = true` → Value pattern `IsReadOnly` is `true`; `SetValue` fails; `false` → `SetValue("abc")` replaces the text
- [x] 3.8 `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` (README §6) and repeat 3.1 – 3.3 on the published (Native AOT) build: same result. **If 3.1 shows no Text pattern or the process crashes under AOT, stop and report: this is the tracer bullet's failure condition.**

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` (README §6) — result reported in the commit body
- [x] No public API change (`PublicAPI.Unshipped.txt` unchanged)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Moving and selecting ranges (P7-18), events, notifications, `ITextProvider2` (P7-19), per-token attributes (colors, bold), annotations.

## Suggested commit message

`feat(winui): expose the editor text through UI Automation (text and value patterns)`
