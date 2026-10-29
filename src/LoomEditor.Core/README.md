# LoomEditor.Core

Platform-independent text model of LoomEditor (piece tree, positions, ranges, selections, undo/redo, decorations, markers, diff), ported from Monaco, with no UI and no Windows dependency.

## Install

```powershell
dotnet add package LoomEditor.Core --prerelease
```

`--prerelease` is needed while the version has a suffix (for example `0.1.0-alpha.1`). Most applications want `LoomEditor.WinUI`, which brings this package and the others.

## More

Part of [LoomEditor](https://github.com/adgranger/LoomEditor), a native code editor control for WinUI 3 ported from the Monaco editor.
Architecture and porting notes: [docs/ARCHITECTURE.md](https://github.com/adgranger/LoomEditor/blob/main/docs/ARCHITECTURE.md),
[docs/PORTING.md](https://github.com/adgranger/LoomEditor/blob/main/docs/PORTING.md). MIT licensed; third-party notices are in `NOTICE.md` of the package.
