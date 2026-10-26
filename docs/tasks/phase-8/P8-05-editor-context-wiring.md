# P8-05 — `EditorContext` and the control expose `LanguageFeatures`, `Markers`, `DocumentUri`; `UiContinuation`

- **Status:** done
- **Depends on:** P8-04
- **Size:** M

## Goal

An application can reach the editor's `LanguageFeaturesService` and `MarkerService` (through the control, `Editor.Markers.Change("my-checker", …)` puts squiggles in the text), the editor knows the URI of its document, features have a thread-safe way to continue
on the UI thread when a provider's `Task` completes, and the language id of the model is easy to read.

## Monaco sources

Sources: none (specified in this card). (Monaco gets services by dependency injection; ADR 0011 decision 1.)

## Files to create or change

- `src/LoomEditor.View/Features/EditorContext.cs` — change: `public LanguageFeaturesService LanguageFeatures { get; } = new();`, `public MarkerService Markers { get; } = new();`, `public string DocumentUri { get; set; }`, `internal IUiDispatcher UiDispatcher { get; }`, `internal MarkerDecorations MarkerDecorations { get; }` created in the constructor, disposed in `Dispose` (hover reads it, P8-09)
- `src/LoomEditor.View/Features/EditorContextExtensions.cs` — change: `internal static` `GetLanguageFeatures`, `GetMarkers`, `GetMarkerDecorations`, `GetUiDispatcher`, `GetDocumentUri`, `GetLanguageId` (see below)
- `src/LoomEditor.View/Features/Scheduling/InlineUiDispatcher.cs` — create (`internal sealed`)
- `src/LoomEditor.View/Features/Scheduling/UiContinuation.cs` — create (`internal static`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.LanguageFeatures.cs` — create: public properties `LanguageFeatures`, `Markers`, `DocumentUri` of the control
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt`, `src/LoomEditor.View/PublicAPI.Unshipped.txt` — regenerate (`./eng/Update-PublicApi.ps1 src/LoomEditor.View`, `src/LoomEditor.WinUI`)
- `tests/LoomEditor.View.Tests/Features/EditorContextTests.cs` — change; `tests/LoomEditor.View.Tests/Features/Scheduling/UiContinuationTests.cs` — create

## Design decisions (already made — do not change)

- **`EditorContext`**: `Markers` is created in the field initializer; `MarkerDecorations` is created **after** `Model` and `Id` exist (end of the constructor): `MarkerDecorations = new MarkerDecorations(Model, Markers, Id);` and disposed first in `Dispose()`.
  `DocumentUri` defaults to `"inmemory://model/" + Id.ToString(CultureInfo.InvariantCulture)`; the setter rejects null / empty (`ArgumentException`) and does nothing else (the LSP session of step F reads it when it opens the document; changing it later is the host's business).
  `UiDispatcher` = the `dispatcher` constructor argument, or `InlineUiDispatcher.Instance` when it is null (the parameterless constructor, tests).
  These are the **public** members of `EditorContext` (it is a public class); `LanguageFeaturesService` and `MarkerService` are the public types of P8-02 / P8-03.
- **Extensions** (same file, same style as `GetAnnouncer`; a context that is not an `EditorContext` gets safe defaults): `GetLanguageFeatures(this IEditorContext)` → `(context as EditorContext)?.LanguageFeatures` (nullable: features that need it do nothing when it is null),
  `GetMarkers` and `GetMarkerDecorations` likewise, `GetUiDispatcher` → `(context as EditorContext)?.UiDispatcher ?? InlineUiDispatcher.Instance`, `GetDocumentUri` → `(context as EditorContext)?.DocumentUri ?? "inmemory://model/0"`,
  `GetLanguageId(this IEditorContext context)` → `context.Model.Tokenization.GetLanguageId()` (`grep -n "GetLanguageId" src/LoomEditor.Core/Model` for the exact member; if it is not reachable from View, use the `IVirtualModel` explicit implementation through a Core `internal` property you add: `TextModel.LanguageId`, one line).
- **`InlineUiDispatcher`**: `internal sealed class InlineUiDispatcher : IUiDispatcher` with `static readonly Instance`; `Post(Action action)` runs `action()` immediately on the calling thread and returns `true` (documented: used when there is no dispatcher; callbacks run on the thread that completes the task). `IUiDispatcher` is the existing `internal` interface of `LoomEditor.Core.Threading`.
- **`UiContinuation`** (the only way View code consumes a provider `Task`, README §4):

  ```csharp
  internal static class UiContinuation
  {
      /// Runs onSuccess(result) through the dispatcher when the task completed with a result and the token is not cancelled;
      /// onError(exception) when it faulted (never for cancellation); nothing at all when the token is cancelled or the task was cancelled.
      public static void Then<T>(Task<T> task, IUiDispatcher dispatcher, Action<T> onSuccess, Action<Exception>? onError = null, CancellationToken token = default);
  }
  ```

  The token is the last parameter because the CA1068 analyzer rejects any other position; callers pass it by name (`token: cts.Token`).

  Implementation: `task.ContinueWith(static (t, state) => …, state, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)`; inside, `dispatcher.Post(() => { if (token.IsCancellationRequested) return; … })`
  (check the token **again** inside the posted action: the request can be cancelled between completion and the post). A `t.IsCanceled` task or an `OperationCanceledException` fault → nothing. `onSuccess` / `onError` exceptions are not caught (they are the caller's bugs).
  No `await`, no `async`. A `Post` that returns `false` (shutting down) drops the result. Null arguments → `ArgumentNullException`. `<remarks>Threading: Then can be called from any thread; the callbacks run on the dispatcher's thread.</remarks>`
- **The control** (`LoomEditor.LanguageFeatures.cs`, `public sealed partial class LoomEditor`): `public LanguageFeaturesService LanguageFeatures => _context.LanguageFeatures;`, `public MarkerService Markers => _context.Markers;`,
  `public string DocumentUri { get => _context.DocumentUri; set => _context.DocumentUri = value; }` (plain properties, **not** dependency properties: the XAML property system adds nothing here and a `DependencyProperty` needs reflection-free registration code for no benefit). XML doc on each.
  Check the `_context` field name in `LoomEditor.cs` (it is `_context`; the dispatcher passed to the constructor already is `new DispatcherQueueUiDispatcher(DispatcherQueue)`).
- Features and widgets read these through the extensions; nothing in this card calls a provider.

## Tests

- [x] New (`EditorContextTests`): `Markers_ProduceDecorations_OnTheModel` (set a marker on `context.Markers`, `context.Model.GetAllDecorations(ownerId: context.Id)`-like query of the existing API shows one `marker-decoration`), `Markers_AreRemovedOnDispose`, `DocumentUri_Default_UsesTheEditorId`,
      `DocumentUri_Empty_Throws`, `LanguageFeatures_IsNotNull_AndPerEditor` (two contexts have different services), `UiDispatcher_Default_IsInline`, `UiDispatcher_Given_IsUsed` (pass a `ManualUiDispatcher`: `GetUiDispatcher()` returns it),
      `GetLanguageId_ReturnsTheModelLanguage` (`context.Model.Tokenization.SetLanguageId("csharp")` → `"csharp"`; plain text by default: `"plaintext"`)
- [x] New (`UiContinuationTests`, uses `ManualUiDispatcher`): `Then_Success_PostsTheResult` (the callback has not run before `RunAll()`), `Then_Faulted_CallsOnError`, `Then_Faulted_NoOnError_DoesNothing`, `Then_CancelledTask_CallsNothing`,
      `Then_TokenCancelledBeforeCompletion_CallsNothing`, `Then_TokenCancelledAfterCompletionBeforeRunAll_CallsNothing`, `Then_PostRefused_DropsTheResult` (a dispatcher whose `Post` returns false), `Then_CompletedFromAnotherThread_CallbackRunsOnRunAllThread`
      (complete a `TaskCompletionSource` from `Task.Run`, wait with a timeout of 5 s for the post, then `RunAll()` on the test thread; the callback runs on the test thread), `Then_InlineDispatcher_RunsSynchronouslyForCompletedTask`
- [x] The control property cards have no unit test (WinUI): the build is the check; create `docs/testing/phase-8-manual-tests.md` (same header block as `phase-7-manual-tests.md`: title "Phase 8 — manual test list", the fill-in lines, a "Setup" section, a "Findings" table at the end) with one setup line: "`Editor.Markers.Change("manual", [new MarkerData(MarkerSeverity.Error, "x", 1, 1, 1, 4)])` puts an error mark in the overview ruler". Later cards add sections; P8-38 writes it in full.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `./eng/Update-PublicApi.ps1` run for `src/LoomEditor.View` and `src/LoomEditor.WinUI`, diffs checked (the three new public members of `EditorContext`, the three of the control)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Calling providers, features, squiggle drawing (P8-06), the LSP session, `IEditorContext` interface changes (features use the extensions).

## Suggested commit message

`feat(view): expose language features, markers and the document uri; add UiContinuation`
