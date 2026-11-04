# LoomEditor.Avalonia

The `LoomEditor` code editor control for Avalonia 12 on Windows, macOS and Linux: native, Native AOT compatible, no web view, ported from the Monaco editor. This is the package
Avalonia applications install.

## Install

```powershell
dotnet add package LoomEditor.Avalonia --prerelease
```

`--prerelease` is needed while the version has a suffix (for example `0.1.0-alpha.1`).

## More

Part of [LoomEditor](https://github.com/adgranger/LoomEditor), a native code editor control ported from the Monaco editor.
Architecture and porting notes: [docs/ARCHITECTURE.md](https://github.com/adgranger/LoomEditor/blob/main/docs/ARCHITECTURE.md),
[docs/PORTING.md](https://github.com/adgranger/LoomEditor/blob/main/docs/PORTING.md). MIT licensed; third-party notices are in `NOTICE.md` of the package.

## Use

Add the styles after your theme in `App.axaml`, then use the control:

```xml
<Application xmlns:loom="using:LoomEditor.Avalonia">
    <Application.Styles>
        <FluentTheme />
        <loom:LoomEditorStyles />
    </Application.Styles>
</Application>
```

```xml
<Window xmlns:loom="using:LoomEditor.Avalonia.Controls">
    <loom:LoomEditor Text="Hello, world!" />
</Window>
```
