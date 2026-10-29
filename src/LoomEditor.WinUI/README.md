# LoomEditor.WinUI

The `LoomEditor` code editor control for WinUI 3: native, Native AOT compatible, no WebView2, ported from the Monaco editor. This is the package most applications install.

## Install

```powershell
dotnet add package LoomEditor.WinUI --prerelease
```

`--prerelease` is needed while the version has a suffix (for example `0.1.0-alpha.1`).

## More

Part of [LoomEditor](https://github.com/adgranger/LoomEditor), a native code editor control for WinUI 3 ported from the Monaco editor.
Architecture and porting notes: [docs/ARCHITECTURE.md](https://github.com/adgranger/LoomEditor/blob/main/docs/ARCHITECTURE.md),
[docs/PORTING.md](https://github.com/adgranger/LoomEditor/blob/main/docs/PORTING.md). MIT licensed; third-party notices are in `NOTICE.md` of the package.

## Use

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:loom="using:LoomEditor.WinUI.Controls">

    <loom:LoomEditor Text="Hello, world!" />
</Window>
```
