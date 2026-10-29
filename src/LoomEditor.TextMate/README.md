# LoomEditor.TextMate

TextMate grammar tokenizer for LoomEditor (port of vscode-textmate) backed by the native Oniguruma regex engine. The package carries `onig.dll` for `win-x64` and `win-arm64` under `runtimes/`.

## Install

```powershell
dotnet add package LoomEditor.TextMate --prerelease
```

`--prerelease` is needed while the version has a suffix (for example `0.1.0-alpha.1`). Most applications want `LoomEditor.WinUI`, which brings this package and the others.

## More

Part of [LoomEditor](https://github.com/adgranger/LoomEditor), a native code editor control for WinUI 3 ported from the Monaco editor.
Architecture and porting notes: [docs/ARCHITECTURE.md](https://github.com/adgranger/LoomEditor/blob/main/docs/ARCHITECTURE.md),
[docs/PORTING.md](https://github.com/adgranger/LoomEditor/blob/main/docs/PORTING.md). MIT licensed; third-party notices are in `NOTICE.md` of the package.
