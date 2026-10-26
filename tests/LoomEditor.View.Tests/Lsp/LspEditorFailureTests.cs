using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Editor;
using LoomEditor.Lsp.FakeServer;
using LoomEditor.Lsp.Tests.TestHelpers;
using LoomEditor.View.Features.GotoDefinition;
using LoomEditor.View.Features.Suggest;

namespace LoomEditor.View.Tests.Lsp;

// New tests: a server that dies, never starts or answers late must leave the editor usable.
public sealed class LspEditorFailureTests
{
    private static bool HasProviders(LspEditorHarness h)
        => h.Context.LanguageFeatures.CompletionProviders.Has("fake")
            || h.Context.LanguageFeatures.HoverProviders.Has("fake")
            || h.Context.LanguageFeatures.DefinitionProviders.Has("fake");

    private static LspEditorHarness OnProcess(FakeServerOptions options, string text = "")
        => LspEditorHarness.OnCommand(FakeServerLocator.GetCommand(options), text);

    private static async Task TypeAndRevealWithoutServerAsync(LspEditorHarness h)
    {
        h.Type("fo");
        h.Context.Commands.Execute(GotoDefinitionCommands.RevealDefinition);
        h.Context.Commands.Execute(SuggestCommands.TriggerSuggest);
        await h.SettleAsync();
        Assert.False(h.Suggest.IsVisible);
    }

    [Fact]
    public async Task ServerCrash_RemovesTheFeatures()
    {
        await using LspEditorHarness h = OnProcess(new FakeServerOptions { ExitOn = "textDocument/completion" }, "ERROR ");
        await h.StartAsync();
        await h.PumpUntilAsync(() => h.Context.Markers.Read().Count == 1, "the marker");
        h.MoveCaret(1, 7);

        h.Type("f");
        await h.PumpUntilAsync(() => h.Session.State == LspSessionState.Faulted, "the session to fault");

        Assert.False(HasProviders(h));
        Assert.Empty(h.Context.Markers.Read());
        Assert.NotNull(h.Session.FaultMessage);
        h.Type("o");
        await h.SettleAsync();
        Assert.Equal("ERROR fo", h.Text);
    }

    [Fact]
    public async Task ServerCrashDuringInitialize_StartFaults_EditorKeepsWorking()
    {
        await using LspEditorHarness h = OnProcess(new FakeServerOptions { ExitOn = "initialize" });

        await Assert.ThrowsAnyAsync<Exception>(() => h.StartAsync());

        Assert.Equal(LspSessionState.Faulted, h.Session.State);
        Assert.False(HasProviders(h));
        await TypeAndRevealWithoutServerAsync(h);
        Assert.Equal("fo", h.Text);
    }

    [Fact]
    public async Task ServerNotFound_StartFaults_EditorKeepsWorking()
    {
        await using LspEditorHarness h = LspEditorHarness.OnCommand(new LspServerCommand("this-does-not-exist-xyz", []));

        await Assert.ThrowsAsync<LspServerStartException>(() => h.StartAsync());

        Assert.Equal(LspSessionState.Faulted, h.Session.State);
        await TypeAndRevealWithoutServerAsync(h);
        Assert.Equal("fo", h.Text);
    }

    [Fact]
    public async Task SlowServer_CompletionArrivesLate_StillShown()
    {
        // In process: the test reads the server's received methods. The delay is real time, the editor clock is the test's.
        await using LspEditorHarness h = LspEditorHarness.InProcess("foo foobar\n", new FakeServerOptions { DelayMs = 300 });
        await h.StartAsync();
        h.MoveCaret(2, 1);

        h.Type("fo");
        await h.SettleAsync(100);
        Assert.False(h.Suggest.IsVisible);
        await h.PumpUntilAsync(() => h.Suggest.IsVisible, "the late completion list");

        Assert.Contains("foobar", h.Labels);
    }

    [Fact]
    public async Task SlowServer_TypingMoreCancelsTheOldRequest()
    {
        await using LspEditorHarness h = LspEditorHarness.InProcess("foo foobar\n", new FakeServerOptions { DelayMs = 300 });
        await h.StartAsync();
        h.MoveCaret(2, 1);

        h.Type("f");
        await h.PumpUntilAsync(() => h.Server!.ReceivedMethods.Contains("textDocument/completion"), "the first request");
        h.Type(" fo");
        h.Command(SuggestCommands.TriggerSuggest);
        await h.PumpUntilAsync(() => h.Server!.ReceivedMethods.Contains("$/cancelRequest"), "the cancellation");
        await h.PumpUntilAsync(() => h.Suggest.IsVisible, "the list of the latest word");

        Assert.Contains("foobar", h.Labels);
        Assert.Contains("for", h.Labels);
    }
}
