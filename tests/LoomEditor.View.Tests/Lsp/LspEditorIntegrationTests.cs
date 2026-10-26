using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Editor;
using LoomEditor.View.Features.GotoDefinition;
using LoomEditor.View.Features.Suggest;
using LoomEditor.View.Hover;

namespace LoomEditor.View.Tests.Lsp;

// New tests: the layers of phase 8 (features, providers, session) against the fake language server; assertions go through the fake presenters.
public sealed class LspEditorIntegrationTests
{
    private const string Words = "foo foobar\n";

    private sealed class RecordingOpener : ILocationOpener
    {
        public List<LocationLink> Opened { get; } = [];

        public bool Open(LocationLink target)
        {
            Opened.Add(target);
            return true;
        }
    }

    private static async Task<LspEditorHarness> StartedAsync(string text = "")
    {
        LspEditorHarness harness = LspEditorHarness.InProcess(text);
        await harness.StartAsync();
        return harness;
    }

    private static async Task TypeAndShowListAsync(LspEditorHarness h, string typed)
    {
        h.Type(typed);
        await h.PumpUntilAsync(() => h.Suggest.IsVisible, "the completion list");
    }

    private static int CompletionRequests(LspEditorHarness h) => h.Server!.ReceivedMethods.Count(method => method == "textDocument/completion");

    private static IReadOnlyList<string> ClassNames(LspEditorHarness h)
        => [.. h.Context.Model.GetAllDecorations(h.Context.Id).Select(d => d.Options.ClassName).Where(c => c is not null && c.StartsWith("squiggly", StringComparison.Ordinal)).Select(c => c!)];

    [Fact]
    public async Task Start_RegistersProviders_AndOpensTheDocument()
    {
        await using LspEditorHarness h = await StartedAsync("hello");

        await h.SyncAsync();

        Assert.True(h.Context.LanguageFeatures.CompletionProviders.Has("fake"));
        Assert.True(h.Context.LanguageFeatures.HoverProviders.Has("fake"));
        Assert.True(h.Context.LanguageFeatures.DefinitionProviders.Has("fake"));
        Assert.Equal(h.Text, h.Server!.DocumentText);
    }

    [Fact]
    public async Task Typing_ShowsTheCompletionListFromTheServer()
    {
        await using LspEditorHarness h = await StartedAsync(Words);
        h.MoveCaret(2, 1);

        await TypeAndShowListAsync(h, "fo");

        Assert.Contains("foo", h.Labels);
        Assert.Contains("foobar", h.Labels);
        Assert.Contains("for", h.Labels);
        Assert.DoesNotContain("else", h.Labels);
        Assert.DoesNotContain("class", h.Labels);
        Assert.Equal("foo", h.Labels[0]);
    }

    [Fact]
    public async Task Typing_FilterNarrows()
    {
        await using LspEditorHarness h = await StartedAsync(Words);
        h.MoveCaret(2, 1);
        await TypeAndShowListAsync(h, "fo");
        int before = h.Labels.Count;

        h.Type("o");
        await h.SettleAsync();

        Assert.True(h.Suggest.IsVisible);
        Assert.True(h.Labels.Count < before);
        Assert.DoesNotContain("for", h.Labels);
        Assert.Equal(1, CompletionRequests(h));
    }

    [Fact]
    public async Task AcceptWithEnter_InsertsTheItem()
    {
        await using LspEditorHarness h = await StartedAsync(Words);
        h.MoveCaret(2, 1);
        await TypeAndShowListAsync(h, "fo");

        h.Command(SuggestCommands.AcceptSelectedSuggestion);

        Assert.Equal("foo foobar\nfoo", h.Text);
        Assert.Equal(Selection.Collapsed(new Position(2, 4)), h.Context.Selection);
        Assert.False(h.Suggest.IsVisible);
        await h.SyncAsync();
        Assert.Equal(h.Text, h.Server!.DocumentText);
    }

    [Fact]
    public async Task AcceptThenUndo_RestoresTheText()
    {
        await using LspEditorHarness h = await StartedAsync(Words);
        h.MoveCaret(2, 1);
        await TypeAndShowListAsync(h, "fo");
        h.Command(SuggestCommands.AcceptSelectedSuggestion);

        h.Context.Model.Undo();

        Assert.Equal("foo foobar\nfo", h.Text);
    }

    [Fact]
    public async Task TriggerDot_ShowsMembers_AcceptReplacesNothing()
    {
        await using LspEditorHarness h = await StartedAsync();

        await TypeAndShowListAsync(h, "x.");

        Assert.Subset(new HashSet<string>(h.Labels), new HashSet<string> { "Length", "Count", "ToString", "Equals" });
        h.Suggest.State!.Select(h.Labels.ToList().IndexOf("Count"));
        h.Command(SuggestCommands.AcceptSelectedSuggestion);
        Assert.Equal("x.Count", h.Text);
    }

    [Fact]
    public async Task AcceptMethod_WithCommitCharacter()
    {
        await using LspEditorHarness h = await StartedAsync();
        await TypeAndShowListAsync(h, "x.To");
        await h.PumpUntilAsync(() => h.Labels.Count > 0 && h.Labels[0] == "ToString", "ToString to be first");

        h.Type("(");

        Assert.Equal("x.ToString(", h.Text);
    }

    [Fact]
    public async Task AcceptLength_AppliesTheLateAdditionalEdit()
    {
        await using LspEditorHarness h = await StartedAsync();
        await TypeAndShowListAsync(h, "x.");
        h.Suggest.State!.Select(h.Labels.ToList().IndexOf("Length"));

        h.Command(SuggestCommands.AcceptSelectedSuggestion);
        await h.PumpUntilAsync(() => h.Text.StartsWith("// resolved", StringComparison.Ordinal), "the late additional edit");

        Assert.Equal("// resolved\nx.Length", h.Text);
        Assert.Equal(Selection.Collapsed(new Position(2, 9)), h.Context.Selection);
    }

    [Fact]
    public async Task ManualTrigger_CtrlSpace_ShowsAllWords()
    {
        await using LspEditorHarness h = await StartedAsync(Words);
        h.MoveCaret(2, 1);

        h.Command(SuggestCommands.TriggerSuggest);
        await h.PumpUntilAsync(() => h.Suggest.IsVisible, "the completion list");

        Assert.Subset(new HashSet<string>(h.Labels), new HashSet<string> { "foo", "foobar", "if", "else", "for", "class" });
    }

    [Fact]
    public async Task Escape_ClosesTheList()
    {
        await using LspEditorHarness h = await StartedAsync(Words);
        h.MoveCaret(2, 1);
        await TypeAndShowListAsync(h, "fo");

        h.Command(SuggestCommands.HideSuggestWidget);

        Assert.False(h.Suggest.IsVisible);
    }

    [Fact]
    public async Task Diagnostics_BecomeMarkersAndSquiggles()
    {
        await using LspEditorHarness h = await StartedAsync();

        h.SetText("ERROR here WARN");
        await h.PumpUntilAsync(() => h.Context.Markers.Read().Count == 2, "two markers");

        Assert.Equal(["squiggly-error", "squiggly-warning"], ClassNames(h).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Diagnostics_FollowTheEdits()
    {
        await using LspEditorHarness h = await StartedAsync();
        h.SetText("ERROR here WARN");
        await h.PumpUntilAsync(() => h.Context.Markers.Read().Count == 2, "two markers");

        h.Context.Model.ApplyEdits([TextEdit.Delete(new Range(1, 1, 1, 6))]);
        await h.PumpUntilAsync(() => h.Context.Markers.Read().Count == 1, "one marker");

        Assert.Equal("Found WARN", h.Context.Markers.Read()[0].Message);
    }

    [Fact]
    public async Task Diagnostics_StaleVersion_NotShown()
    {
        await using LspEditorHarness h = await StartedAsync();

        h.SetText("ERROR");
        h.SetText("WARN");
        await h.PumpUntilAsync(() => h.Context.Markers.Read().Any(m => m.Message == "Found WARN"), "the diagnostics of the last text");
        await h.SettleAsync();

        Marker marker = Assert.Single(h.Context.Markers.Read());
        Assert.Equal("Found WARN", marker.Message);
    }

    [Fact]
    public async Task Hover_ShowsProviderMarkdown()
    {
        await using LspEditorHarness h = await StartedAsync("alpha beta");

        h.MoveMouseTo(1, 8);
        await h.PumpUntilAsync(() => MarkdownText(h) is not null, "the hover");

        Assert.Contains("Hover for **beta**", MarkdownText(h));
    }

    [Fact]
    public async Task Hover_OnMarkerWord_ShowsMarkerThenProvider()
    {
        await using LspEditorHarness h = await StartedAsync("ERROR here");
        await h.PumpUntilAsync(() => h.Context.Markers.Read().Count == 1, "the marker");

        h.MoveMouseTo(1, 3);
        await h.PumpUntilAsync(() => MarkdownText(h) is not null, "the hover");

        IReadOnlyList<IHoverPart> parts = h.Hover.Last!.Parts;
        Assert.Equal("Found ERROR", Assert.IsType<MarkerHoverPart>(parts[0]).Marker.Message);
        Assert.IsType<MarkdownHoverPart>(parts[1]);
    }

    [Fact]
    public async Task Hover_LeaveHides()
    {
        await using LspEditorHarness h = await StartedAsync("alpha beta");
        h.DisableStickyHover();
        h.MoveMouseTo(1, 8);
        await h.PumpUntilAsync(() => MarkdownText(h) is not null, "the hover");

        h.Context.UserInputEvents.EmitMouseLeave();
        await h.PumpUntilAsync(() => !h.Hover.IsVisible, "the hover to hide");

        Assert.False(h.Hover.IsVisible);
    }

    [Fact]
    public async Task F12_GoesToTheDeclaration()
    {
        await using LspEditorHarness h = await StartedAsync("def alpha\nalpha");
        h.MoveCaret(2, 2);

        h.Command(GotoDefinitionCommands.RevealDefinition);
        await h.PumpUntilAsync(() => h.Context.Selection.Range.StartLineNumber == 1, "the jump");

        Assert.Equal(Selection.Collapsed(new Position(1, 5)), h.Context.Selection);
    }

    [Fact]
    public async Task CtrlClick_GoesToTheClassDeclaration()
    {
        await using LspEditorHarness h = await StartedAsync("class Beta\nBeta");
        h.MoveCaret(2, 2);

        h.CtrlClick(2, 2);
        await h.PumpUntilAsync(() => h.Context.Selection.Range.StartLineNumber == 1, "the jump");

        Assert.Equal(1, h.Context.Selection.Range.StartLineNumber);
        Assert.Equal(7, h.Context.Selection.Range.StartColumn);
    }

    [Fact]
    public async Task F12_ExternalDefinition_CallsTheLocationOpener()
    {
        await using LspEditorHarness h = await StartedAsync("external");
        RecordingOpener opener = new();
        h.Context.LocationOpener = opener;
        h.MoveCaret(1, 3);

        h.Command(GotoDefinitionCommands.RevealDefinition);
        await h.PumpUntilAsync(() => opener.Opened.Count == 1, "the opener call");

        Assert.Equal("file:///other/file.txt", opener.Opened[0].Uri);
    }

    [Fact]
    public async Task F12_NoDefinition_Announces()
    {
        await using LspEditorHarness h = await StartedAsync("nothing");
        h.MoveCaret(1, 3);

        h.Command(GotoDefinitionCommands.RevealDefinition);
        await h.PumpUntilAsync(() => h.Announcer.Messages.Count > 0, "the announcement");

        Assert.NotEmpty(h.Announcer.Messages);
    }

    [Fact]
    public async Task Stop_RemovesProvidersMarkersAndSquiggles_AndTheServerSawShutdown()
    {
        await using LspEditorHarness h = await StartedAsync("ERROR here");
        await h.PumpUntilAsync(() => h.Context.Markers.Read().Count == 1, "the marker");

        Task stop = h.Session.StopAsync();
        await h.PumpUntilAsync(() => stop.IsCompleted, "the stop");
        await stop;

        Assert.Equal(LspSessionState.Stopped, h.Session.State);
        Assert.False(h.Context.LanguageFeatures.CompletionProviders.Has("fake"));
        Assert.False(h.Context.LanguageFeatures.HoverProviders.Has("fake"));
        Assert.False(h.Context.LanguageFeatures.DefinitionProviders.Has("fake"));
        Assert.Empty(h.Context.Markers.Read());
        Assert.Empty(ClassNames(h));
        await h.Host!.ServerExit.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Contains("shutdown", h.Server!.ReceivedMethods);
    }

    [Fact]
    public async Task AfterStop_TypingDoesNotShowTheList()
    {
        await using LspEditorHarness h = await StartedAsync(Words);
        Task stop = h.Session.StopAsync();
        await h.PumpUntilAsync(() => stop.IsCompleted, "the stop");
        await stop;
        h.MoveCaret(2, 1);

        h.Type("fo");
        await h.SettleAsync();

        Assert.False(h.Suggest.IsVisible);
    }

    [Fact]
    public async Task TextEditsAreSynced_ThroughTheWholeStack()
    {
        await using LspEditorHarness h = await StartedAsync("one two\nthree");
        Random random = new(7);

        for (int i = 0; i < 50; i++)
        {
            if (random.Next(3) == 0 && h.Text.Length > 0)
            {
                h.Command("deleteLeft");
            }
            else
            {
                h.Type(((char)('a' + random.Next(26))).ToString());
            }

            if (i % 10 == 9)
            {
                await h.SettleAsync(30);
            }
        }

        await h.SyncAsync();
        Assert.True(h.Server!.DocumentText == h.Text, "seed 7: the server text differs from the model text");
    }

    private static string? MarkdownText(LspEditorHarness h)
        => h.Hover.IsVisible
            ? h.Hover.Last!.Parts.OfType<MarkdownHoverPart>().SelectMany(p => p.Contents).Select(c => c.Value).FirstOrDefault()
            : null;
}
