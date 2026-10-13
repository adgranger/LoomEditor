# Phase 0 — Skeleton, docs, CI, AOT smoke test

**Done when:** CI is green and the Native AOT Gallery starts.

- [x] Solution structure (`src/`, `tests/`, `samples/`, `docs/`), shared build settings, central package versions
- [x] Baseline checks recorded in ADR 0001 (SDK, Windows App SDK, AOT, Oniguruma) and ADR 0002 (rendering backend)
- [x] `Position`, `Range`, `Selection`, `TextEdit` with tests (Monaco `range.test.ts` ported)
- [x] `IEditorFeature`, `IEditorContext`, `EditorFeatures`, `CommandRegistry`, example feature `SelectAll`
- [x] `LoomEditor` control showing static text in the Gallery (Win2D), published with `PublishAot=true`
- [x] Architecture tests, CI workflow, Oniguruma build script
- [ ] First CI run green on GitHub (x64 + ARM64)
