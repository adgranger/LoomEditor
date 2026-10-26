# P8-37 — Gallery: connect to a real language server, demo providers and the LSP self-check

- **Status:** done
- **Depends on:** P8-36
- **Size:** M
- **Recommended model:** Opus (a WinUI app under Native AOT that starts a child process, a UI-thread marshalling path from a read loop, and a real server (`csharp-ls`) whose behavior on this SDK could not be verified when the card was written. On failure — the published exe crashes or hangs, the self-check fails against the fake server, a XamlCompiler / trimming error you cannot explain — stop, do not commit, report the command, the output, `%TEMP%\LoomEditor.Gallery.log` and what you tried)

## Goal

The Gallery shows the language features: **Start language server** connects the editor to `csharp-ls` (when installed) or to any server named by an environment variable, with a status line and a log; **Demo providers** registers tiny in-process providers (completion, hover, definition) and **Demo markers** adds squiggles, so every widget can be tried without any server;
and a command-line **self-check** drives the whole stack in the real (Native AOT) app against the fake server and writes a pass / fail report. What only a human with the real server can verify stays marked pending.

## Monaco sources

Sources: none (specified in this card; ADR 0011 decision 9 for the server, ADR 0010's `UiaSelfCheck` pattern for the self-check).

## Files to create or change

- `samples/LoomEditor.Gallery/LoomEditor.Gallery.csproj` — change: `ProjectReference` to `..\..\src\LoomEditor.Lsp\LoomEditor.Lsp.csproj`
- `samples/LoomEditor.Gallery/Lsp/LspServerLocator.cs`, `LspDemoWorkspace.cs`, `DemoProviders.cs`, `DemoMarkers.cs`, `GalleryLocationOpener.cs`, `LspDemoController.cs`, `LspSelfCheck.cs`, `LspDemoSample.cs` — create (namespace `LoomEditor.Gallery.Lsp`; **`LspServerLocator`, `LspDemoWorkspace`, `DemoProviders` and `DemoMarkers` use no WinUI types**: they are linked into the Lsp test project)
- `samples/LoomEditor.Gallery/MainWindow.xaml` and `MainWindow.Lsp.cs` (a new `partial` file; the XAML part follows the style of the existing buttons: event attributes, code-behind, **no `{Binding}`**) — change / create
- `tests/LoomEditor.Lsp.Tests/LoomEditor.Lsp.Tests.csproj` — change: `<Compile Include="..\..\samples\LoomEditor.Gallery\Lsp\LspServerLocator.cs;…\LspDemoWorkspace.cs;…\DemoProviders.cs;…\DemoMarkers.cs" Link="Gallery\%(Filename)%(Extension)" />`
- `tests/LoomEditor.Lsp.Tests/Gallery/LspServerLocatorTests.cs`, `LspDemoWorkspaceTests.cs`, `DemoProvidersTests.cs` — create
- `docs/testing/phase-8-manual-tests.md` — change: section 4 below; `samples/LoomEditor.Gallery/README.md` (if absent, add the "Language server" section to the Gallery's header comment in `MainWindow.Lsp.cs`)

## Design decisions (already made — do not change)

- **`LspServerLocator`** (`internal static class`; pure so that it is testable): `static LspServerChoice? Find(Func<string, string?> getEnvironmentVariable, Func<string, bool> fileExists, string userProfileFolder, IEnumerable<string> pathFolders)` and `static LspServerChoice? Find()` (the real one: `Environment.GetEnvironmentVariable`, `File.Exists`, `Environment.GetFolderPath(UserProfile)`, the `PATH` split on `Path.PathSeparator`).
  `sealed record LspServerChoice(string FileName, IReadOnlyList<string> Arguments, string LanguageId, string Description)`. Order (ADR 0011 decision 9): (1) `LOOMEDITOR_LSP_COMMAND` set and non-empty → `SplitCommandLine(value)` (first token the file name, the rest the arguments; tokens separated by spaces, **double quotes group** a token with spaces and are removed; no escapes; an empty result → ignore the variable), `LanguageId` = `LOOMEDITOR_LSP_LANGUAGE` or `"csharp"`, `Description = "LOOMEDITOR_LSP_COMMAND"`;
  (2) `<userProfile>\.dotnet\tools\csharp-ls.exe` exists (`Path.Combine`; on non-Windows `csharp-ls`) → `csharp-ls`, no arguments, language `csharp`, description `"csharp-ls (dotnet tool)"`; (3) the first `csharp-ls.exe` (`csharp-ls` off Windows) found in `pathFolders` → same; (4) `null`. `Find` never throws (an invalid path in `PATH` is skipped).
  `internal static string InstallHint => "dotnet tool install --global csharp-ls"`.
- **`LspDemoWorkspace`** (pure file work): `static LspDemoWorkspaceInfo Create(string baseFolder, string programText)` writes `<baseFolder>\lsp-workspace\Sample.csproj` (`<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>` — `net8.0`: any installed SDK can build it, and csharp-ls only needs it to load) and `Program.cs` = `programText` (UTF-8 without BOM),
  overwriting both each time; returns `LspDemoWorkspaceInfo(string Folder, string FolderUri, string ProgramPath, string ProgramUri)` with file URIs built with `new Uri(path).AbsoluteUri` (`file:///C:/…`). The default `baseFolder` is `%LOCALAPPDATA%\LoomEditor.Gallery`. The folder is created if missing; an I/O failure throws `IOException` to the caller (the controller shows it).
- **`LspDemoSample.Text`** (a C# string constant, the text the demo and the self-check use):

  ```csharp
  using System;

  class Program
  {
      static void Greet()
      {
          Console.WriteLine("Hello"); // ERROR
      }

      static void Main()
      {
          int number = "text";
          Program.Greet();
          Console.
      }
  }
  ```
  (A real type error for `csharp-ls`, the word `ERROR` for the fake server, `class Program` + a use of `Program` for definition, and an open `Console.` for completion. Line 14, column 17 is right after `Console.`; `Program` of line 13 is at column 9.)
- **`DemoProviders`** (`public`-free, `internal static class` + three `internal sealed class`es, Core API only): `DemoCompletionProvider : ICompletionItemProvider` (trigger character `.`; returns the distinct identifier words of the document longer than 1 character, sorted ordinal, kind `Text`, plus the keywords `if else for while return class static void int string` as kind `Keyword`; `Task.FromResult`),
  `DemoHoverProvider : IHoverProvider` (the word at the position → a `Hover` with `MarkdownString("**<word>**\n\nDemo hover from an in-process provider.")` and the word's range; none → `null`), `DemoDefinitionProvider : IDefinitionProvider` (the first line containing `class <word>`, `struct <word>`, `void <word>` or `int <word>` as whole words → one `LocationLink` with `Uri = documentUri` (given to the constructor) and the declared word's range; none → empty list).
  `static IDisposable Register(LanguageFeaturesService features, string languageId, string documentUri)` registers the three and returns a disposable that unregisters them. They read the model **synchronously** in the provider call (UI thread, before returning a completed task: allowed because they are called on the UI thread and do not await).
- **`DemoMarkers.Set(MarkerService markers, TextModel model)`**: `markers.Change("gallery-demo", …)` with four markers on the first four non-empty lines: Error (message `Demo error`), Warning (`Demo warning`), Info (`Demo info`), Hint (`Demo hint`), each on the first word of its line; `Clear(markers)`: `markers.Remove("gallery-demo")`.
- **`LspDemoController`** (WinUI side, `internal sealed class`, UI thread): `LspDemoController(LoomEditorControl editor, Action<string> setStatus, Action<string> appendLog)`; `Task StartAsync()`: `choice = LspServerLocator.Find()`; none → `setStatus($"Language server not found. Install it with: {InstallHint}")` (or the env var hint) and return (nothing else happens; the editor works as before);
  else `info = LspDemoWorkspace.Create(default folder, editor.Text)` (the **current** editor text is the document; the Gallery language is set to `choice.LanguageId`), `editor.DocumentUri = info.ProgramUri`, `editor.LanguageId = choice.LanguageId`, creates `new LspEditorSession(editor.Model, editor.LanguageFeatures, editor.Markers, info.ProgramUri, choice.LanguageId, new LspServerCommand(choice.FileName, choice.Arguments, info.Folder), action => editor.DispatcherQueue.TryEnqueue(() => action()), new LspSessionOptions { RootUri = info.FolderUri, ClientVersion = "gallery" })`,
  wires `StateChanged` (status text: `Starting…`, `Running (<description>)`, `Stopping…`, `Stopped`, `Faulted: <FaultMessage>`) and `ServerOutput` (`appendLog`), `await session.StartAsync()` inside try / catch (any exception → status `Failed to start: <message>`, no rethrow). `Task StopAsync()`; `bool IsRunning`. The window's `Closed` event calls `StopAsync()` and waits for it for at most **3 seconds** (`Task.WaitAny`) so no server process is left behind.
  `GalleryLocationOpener : ILocationOpener`: `Open(link)` → sets the status `"Definition in <uri> (the Gallery does not open other documents)"` and returns `true`.
- **UI** (`MainWindow.xaml`): a new row of controls next to the existing ones: `Button` **Start language server** (`OnStartLsp`), **Stop language server** (`OnStopLsp`), a `ToggleButton`-less pair **Demo providers** (`OnDemoProviders`: toggles registration, label shows the state) and **Demo markers** (`OnDemoMarkers`: toggles), **Load LSP sample** (`OnLoadLspSample`: `Editor.LanguageId = "csharp"`, `Editor.Text = LspDemoSample.Text`), a `TextBlock` `LspStatus` and a read-only multi-line `TextBox` `LspLog` (height 80, `TextWrapping` off, newest line last, keeps at most the last 200 lines).
  The handlers live in `MainWindow.Lsp.cs`. Start is disabled while running; Stop while not. `Editor.LocationOpener = new GalleryLocationOpener(...)` is set once in the window constructor.
- **Self-check** (`LspSelfCheck`): when the process was started with the argument `--lsp-selfcheck=<file>` (read with `Environment.GetCommandLineArgs()` in the `MainWindow` constructor, executed after the first `Activated`/`Loaded` event), the window runs `LspSelfCheck.RunAsync(editor, outFile)` and then calls `Environment.Exit(failures == 0 ? 0 : 1)`.
  It sets `Editor.Text = LspDemoSample.Text`, `LanguageId = "csharp"`, starts a `LspDemoController` (same code path as the button), and checks, each with its own timeout and recorded as `PASS <name>` or `FAIL <name>: <detail>` lines in `<file>` (UTF-8, written at the end **and** after each check so a hang leaves a partial report):
  `server found` (the locator returned a choice), `session running` (≤ 60 s), `diagnostics` (`editor.Markers.Read().Count >= 1` within 15 s), `completion` (call `editor.LanguageFeatures.CompletionProviders.Ordered("csharp")[0].ProvideCompletionItemsAsync(editor.Model, new Position(14, 17), new CompletionContext(CompletionTriggerKind.TriggerCharacter, "."), token)`; result has at least one item, within 15 s),
  `hover` (`HoverProviders` at `Position(13, 9)`: non-null with non-empty contents), `definition` (`DefinitionProviders` at `Position(13, 9)`: at least one link), `stop` (`StopAsync` completes ≤ 10 s and `editor.Markers.Read().Count == 0` afterwards). A failing check does not stop the following ones except that everything after a failed `session running` is `FAIL … skipped`.
  The calls are made on the UI thread (the provider contract); the awaits use `ConfigureAwait(true)` in this WinUI code.
- **No server found**: the self-check reports `FAIL server found` and exits 1 (it is a verification tool, not a feature).
- **Native AOT**: `LspEditorSession`, `Process`, `System.Text.Json` source generation, the providers: no reflection anywhere in the Gallery additions; no `{Binding}`; the publish is warning-clean.

## Tests

- [x] New (`LspServerLocatorTests`; the linked file; fake environment / file system delegates): `EnvVar_Wins_OverFiles`, `EnvVar_QuotedPathWithSpaces` (`"C:\Program Files\x\server.exe" --stdio` → file name without quotes, one argument), `EnvVar_Language_FromSecondVariable`, `EnvVar_Empty_Ignored`, `EnvVar_OnlyWhitespace_Ignored`, `DotnetToolsFolder_Found`, `Path_Found_WhenNotInDotnetTools`, `NothingFound_Null`, `Find_InvalidPathFolder_Skipped` (a folder name with invalid characters does not throw), `SplitCommandLine_Cases` (a theory: empty, one token, two tokens, quotes, adjacent quotes, trailing space)
- [x] New (`LspDemoWorkspaceTests`, a temp folder per test, deleted at the end): `Create_WritesBothFiles`, `Create_ProgramTextIsExact_NoBom`, `Create_Overwrites`, `Create_UrisAreFileUris` (start with `file:///`, the program uri ends with `Program.cs`), `Create_FolderWithSpaces_UriEncoded`
- [x] New (`DemoProvidersTests`): `Completion_WordsAndKeywords_Sorted`, `Completion_DotTrigger_Items`, `Hover_OnWord`, `Hover_NoWord_Null`, `Definition_Class_Found`, `Definition_NotFound_Empty`, `Register_AndDispose_UpdatesTheRegistries`, `DemoMarkers_Set_FourSeverities`, `DemoMarkers_Clear`, `DemoMarkers_SkipsEmptyLines`
- [x] No unit tests for the XAML / controller; the self-check and the manual rows verify them

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` succeeds, and the published `LoomEditor.Gallery.exe` is still running after 10 seconds (README §7), then killed. Record the result in this card
- [x] **The self-check against the fake server, in the published Native AOT app**: publish the fake server too (`dotnet publish tests/LoomEditor.Lsp.FakeServer -c Release -r win-x64 -p:PublishAot=true -warnaserror`), then run
      `$env:LOOMEDITOR_LSP_COMMAND = '<full path of the published LoomEditor.Lsp.FakeServer.exe>'; Start-Process <published LoomEditor.Gallery.exe> -ArgumentList '--lsp-selfcheck=<absolute path of a report file>' -Wait`
      and read the report: every line is `PASS` (`server found`, `session running`, `diagnostics`, `completion`, `hover`, `definition`, `stop`) and the exit code is 0. Paste the report in this card. If it does not pass: stop and report (this card's Opus instruction). Afterwards check that no `LoomEditor.Gallery` or `LoomEditor.Lsp.FakeServer` process is left (kill them if so)
- [x] The same self-check run **without** `LOOMEDITOR_LSP_COMMAND` on a machine **without** `csharp-ls`: the report says `FAIL server found` and the status text names the install command (run it and record it; if `csharp-ls` is installed on the machine, say so and mark the row `n/a`)
- [x] If `csharp-ls` **is** installed (`Get-Command csharp-ls` / `~\.dotnet\tools`), run the self-check once against it (no environment variable) and **paste the report as it is, pass or fail**: a failure here is information for the maintainer (SDK compatibility, start time), not a reason to change this card; note it under "Open risks" in the card. If it is not installed, write "csharp-ls not installed: not verified" — **never** claim it
- [x] `docs/testing/phase-8-manual-tests.md` section 4 added (below); the maintainer-only rows stay `pending`
- [x] Card status and `docs/tasks/README.md` updated

## Manual checks (add as "4. Language server in the Gallery"; every row needs a human; unverified rows are `pending`)

| # | Steps | Expected |
|---|---|---|
| 4.1 | `dotnet tool install --global csharp-ls`; run the Gallery; **Load LSP sample**; **Start language server** | The status goes `Starting…` then `Running (csharp-ls (dotnet tool))`; no window pops up for the server process |
| 4.2 | Wait a few seconds after starting | A squiggle under `"text"` (error: cannot convert string to int) and a mark in the overview ruler; hovering it shows the compiler message |
| 4.3 | Hover `Console` | A popup with the type's documentation (Markdown rendered as text) |
| 4.4 | Place the caret after `Console.` and press `Ctrl+Space`; type `Wr` | A list of members, `WriteLine` among them; `Enter` inserts it |
| 4.5 | Type `Console.` anew | The list opens by itself right after the `.` |
| 4.6 | `F12` on `Program` in `Program.Greet();` (and `Ctrl+click`) | The caret jumps to the declaration `class Program` |
| 4.7 | **Stop language server** | Status `Stopped`, the squiggles are gone, no `csharp-ls` process remains (`Get-Process csharp-ls*`) |
| 4.8 | Start, then kill the server in Task Manager | Status `Faulted: …`, squiggles gone, the editor still types normally; **Start** works again |
| 4.9 | Uninstall / rename `csharp-ls` and press **Start language server** | The status says the server was not found and shows the install command; nothing else changes |
| 4.10 | Close the window while the server runs | No `csharp-ls` process remains |
| 4.11 | Set `LOOMEDITOR_LSP_COMMAND` to another stdio server and `LOOMEDITOR_LSP_LANGUAGE` to its language id, start | The Gallery uses it (status description `LOOMEDITOR_LSP_COMMAND`) |
| 4.12 | **Demo providers** then type, hover, `F12` (no server) | The widgets of sections 2 and 3 work with the in-process providers; **Demo markers** shows four squiggle kinds |

## Out of scope

Bundling any server, installing `csharp-ls` for the user, restart on crash, multiple documents, a settings dialog, opening other files on `F12`, hover / completion with a server that needs `workspace/configuration` answers beyond the null ones.

## Suggested commit message

`feat(gallery): connect the editor to a language server and add the LSP self-check`

## Result (recorded when the card was done)

- Build Release 0 warnings, `dotnet test --solution` all green (`--max-threads 2`).
- `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` succeeds; the exe was still running after 10 s, then killed.
- Self-check against the published fake server (Native AOT), exit code 0:
  `PASS server found`, `PASS session running`, `PASS diagnostics`, `PASS completion`, `PASS hover`, `PASS definition`, `PASS stop`.
- Without `LOOMEDITOR_LSP_COMMAND`: `csharp-ls` 0.26.0.0 **is installed** on this machine, so the "server not found" row is `n/a` here
  (the not-found path is a status text only; manual row 4.9).
- Self-check against the real `csharp-ls` (no environment variable), exit code 1:

  ```
  PASS server found
  PASS session running
  PASS diagnostics
  FAIL completion: no item
  PASS hover
  PASS definition
  PASS stop
  ```

## Open risks

- `csharp-ls` 0.26.0.0 returned no completion item at line 14 column 17 (right after `Console.`) within the self-check: the document has a syntax
  error there and the server may still be loading the workspace. Information for the maintainer (row 4.4 / 4.5), not changed in this card.
- Deviation: the window `Closed` handler cancels the close (`Handled = true`), waits at most 3 s with `Task.WaitAsync` and closes again, instead
  of blocking the UI thread with `Task.WaitAny` (the session needs the UI thread to finish stopping; CA2027 forbids `WhenAny` + `Delay`).
- The Gallery csproj got the global `Range` alias (the repo convention); the local alias in `ModelDemoActions.cs` was removed.