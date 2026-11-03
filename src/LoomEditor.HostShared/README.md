# LoomEditor.HostShared

Source files compiled into **both** `LoomEditor.WinUI` and `LoomEditor.Avalonia` (no project file; each host imports
`LoomEditor.HostShared.props`). Each assembly declares the types in its own namespace, so both public APIs are unchanged.
See [ADR 0017](../../docs/adr/0017-shared-host-logic.md) and [phase 16](../../docs/tasks/phase-16/README.md).

Every file has one header block, the only conditional code allowed here (checked by `HostSharedSourceTests`):

```csharp
#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif
```

Host members: anything that names a framework type (focus, pointer capture, brushes, dispatching, ...) goes to
`<File>.Host.cs` in each host, with the same name and signature in both. No `Avalonia.*`, `Microsoft.UI.*` or `Windows.*` here.
