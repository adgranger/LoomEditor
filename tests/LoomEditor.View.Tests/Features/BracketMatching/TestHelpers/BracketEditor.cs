// Ported from microsoft/vscode: src/vs/editor/contrib/bracketMatching/test/browser/bracketMatching.test.ts (createTextModelWithBrackets, createCodeEditorWithBrackets)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.View.Features;
using LoomEditor.View.Features.BracketMatching;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Features.BracketMatching.TestHelpers;

/// <summary>An editor whose model has the language <c>bracketMode</c> (brackets <c>{}</c>, <c>[]</c>, <c>()</c>) and a bracket matching feature.</summary>
/// <remarks>Threading: use from one test thread.</remarks>
internal sealed class BracketEditor : IDisposable
{
    private readonly EditorFeatureHost _host;

    public BracketEditor(string text, bool focused = true)
    {
        Scheduler = new ManualEditorScheduler();
        Context = new EditorContext(Scheduler);
        Context.RegisterLanguage("bracketMode");
        Context.Model.LanguageConfigurationRegistry.Register("bracketMode", new LanguageConfiguration
        {
            Brackets = [new("{", "}"), new("[", "]"), new("(", ")")],
        });
        Context.Model.Tokenization.SetLanguageId("bracketMode");
        Context.SetText(text);
        Context.ViewModel.SetHasFocus(focused);
        Feature = new BracketMatchingFeature();
        _host = new EditorFeatureHost(Context, [() => Feature]);
    }

    public ManualEditorScheduler Scheduler { get; }

    public EditorContext Context { get; }

    public BracketMatchingFeature Feature { get; }

    public void Dispose()
    {
        _host.Dispose();
        Context.Dispose();
    }
}
