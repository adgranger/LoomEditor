# LoomEditor.HostShared

Source files compiled into **both** `LoomEditor.WinUI` and `LoomEditor.Avalonia` (no project file; each host imports
`LoomEditor.HostShared.props`). Each assembly declares the types in its own namespace, so both public APIs are unchanged.
See [ADR 0017](../../docs/adr/0017-shared-host-logic.md) and [phase 16](../../docs/tasks/phase-16/README.md).

Every file has one header block, the only conditional code allowed here (checked by `HostSharedSourceTests`). The WinUI branch is the default so that tools parsing without the host symbols (SonarCloud) still see a namespace:

```csharp
#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#endif
```

Host members: anything that names a framework type (focus, pointer capture, brushes, dispatching, ...) goes to
`<File>.Host.cs` in each host, with the same name and signature in both. No `Avalonia.*`, `Microsoft.UI.*` or `Windows.*` here.

No block of 100 tokens may be copied between the two hosts or the two Galleries (`HostTwinDuplicationTests`); move the code to `src/LoomEditor.HostShared/` instead.
