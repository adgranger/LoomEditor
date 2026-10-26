using System.Text;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Editor;
using LoomEditorControl = LoomEditor.WinUI.Controls.LoomEditor;

namespace LoomEditor.Gallery.Lsp;

/// <summary>Drives the whole language server stack in the real app (the Native AOT build included) and writes a PASS / FAIL report.</summary>
/// <remarks>Threading: UI thread only (the provider contract); the awaits resume on the UI thread.</remarks>
internal static class LspSelfCheck
{
    /// <summary>Runs every check and returns the number of failures. The report file is rewritten after each check.</summary>
    internal static async Task<int> RunAsync(LoomEditorControl editor, string reportFile, Action<string> setStatus, Action<string> appendLog)
    {
        StringBuilder report = new();
        int failures = 0;

        void Record(string name, string? failure)
        {
            if (failure is not null)
            {
                failures++;
            }

            report.AppendLine(failure is null ? $"PASS {name}" : $"FAIL {name}: {failure}");
            try
            {
                File.WriteAllText(reportFile, report.ToString(), new UTF8Encoding(false));
            }
            catch (IOException)
            {
                // The report is best effort; the exit code still tells.
            }
        }

        async Task Check(string name, Func<CancellationToken, Task<string?>> check, TimeSpan timeout)
        {
            using CancellationTokenSource cts = new(timeout);
            try
            {
                Record(name, await check(cts.Token).WaitAsync(timeout).ConfigureAwait(true));
            }
            catch (TimeoutException)
            {
                Record(name, $"timeout after {timeout.TotalSeconds:0} s");
            }
            catch (Exception e)
            {
                Record(name, $"{e.GetType().Name}: {e.Message}");
            }
        }

        editor.Text = string.Empty;
        editor.LanguageId = "csharp";
        editor.Text = LspDemoSample.Text;
        LspDemoController controller = new(editor, setStatus, appendLog);
        string language = "csharp";

        LspServerChoice? choice = LspServerLocator.Find();
        if (choice is null)
        {
            // The same code path as the button: it reports the install hint in the status.
            await controller.StartAsync().ConfigureAwait(true);
            Record("server found", $"not found; install it with: {LspServerLocator.InstallHint}");
            return failures;
        }

        language = choice.LanguageId;
        Record("server found", null);
        await controller.StartAsync().ConfigureAwait(true);
        await Check("session running", async token =>
        {
            while (controller.State != LspSessionState.Running)
            {
                if (controller.State is LspSessionState.Faulted or LspSessionState.Stopped)
                {
                    return $"state {controller.State}";
                }

                await Task.Delay(50, token).ConfigureAwait(true);
            }

            return null;
        }, TimeSpan.FromSeconds(60));

        if (controller.State != LspSessionState.Running)
        {
            foreach (string name in new[] { "diagnostics", "completion", "hover", "definition" })
            {
                Record(name, "skipped: the session is not running");
            }
        }
        else
        {
            await Check("diagnostics", async token =>
            {
                while (editor.Markers.Read().Count < 1)
                {
                    await Task.Delay(50, token).ConfigureAwait(true);
                }

                return null;
            }, TimeSpan.FromSeconds(15));
            await Check("completion", async token =>
            {
                ICompletionItemProvider provider = editor.LanguageFeatures.CompletionProviders.Ordered(language)[0];
                CompletionList? list = await provider.ProvideCompletionItemsAsync(
                    editor.Model, new Position(14, 17), new CompletionContext(CompletionTriggerKind.TriggerCharacter, "."), token).ConfigureAwait(true);
                return list is { Suggestions.Count: > 0 } ? null : "no item";
            }, TimeSpan.FromSeconds(15));
            await Check("hover", async token =>
            {
                Hover? hover = await editor.LanguageFeatures.HoverProviders.Ordered(language)[0]
                    .ProvideHoverAsync(editor.Model, new Position(13, 9), token).ConfigureAwait(true);
                return hover is not null && !MarkdownString.IsNullOrEmpty(hover.Contents) ? null : "no hover";
            }, TimeSpan.FromSeconds(15));
            await Check("definition", async token =>
            {
                IReadOnlyList<LocationLink>? links = await editor.LanguageFeatures.DefinitionProviders.Ordered(language)[0]
                    .ProvideDefinitionAsync(editor.Model, new Position(13, 9), token).ConfigureAwait(true);
                return links is { Count: > 0 } ? null : "no link";
            }, TimeSpan.FromSeconds(15));
        }

        await Check("stop", async _ =>
        {
            await controller.StopAsync().ConfigureAwait(true);
            return editor.Markers.Read().Count == 0 ? null : "markers remain";
        }, TimeSpan.FromSeconds(10));
        return failures;
    }
}
