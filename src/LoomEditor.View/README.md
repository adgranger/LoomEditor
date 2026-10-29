# LoomEditor.View

Editor logic of LoomEditor that is not pixels: cursors, commands, view layout and the editor features (find, folding, suggest, snippets, comment threads, diff…), independent of any UI framework.

## Install

```powershell
dotnet add package LoomEditor.View --prerelease
```

`--prerelease` is needed while the version has a suffix (for example `0.1.0-alpha.1`). Most applications want `LoomEditor.WinUI`, which brings this package and the others.

## More

Part of [LoomEditor](https://github.com/adgranger/LoomEditor), a native code editor control for WinUI 3 ported from the Monaco editor.
Architecture and porting notes: [docs/ARCHITECTURE.md](https://github.com/adgranger/LoomEditor/blob/main/docs/ARCHITECTURE.md),
[docs/PORTING.md](https://github.com/adgranger/LoomEditor/blob/main/docs/PORTING.md). MIT licensed; third-party notices are in `NOTICE.md` of the package.
