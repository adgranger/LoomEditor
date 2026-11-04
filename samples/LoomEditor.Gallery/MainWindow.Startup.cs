using LoomEditor.Core.Text;

namespace LoomEditor.Gallery;

public sealed partial class MainWindow
{
    private readonly List<string> _injectedTextIds = [];

    /// <summary>Fills the pickers, loads the sample text and wires the model events (same calls in both Galleries).</summary>
    private void InitializeEditor()
    {
        PopulatePickers();
        InitializeComments();
        Editor.LanguageId = "csharp";
        LanguageBox.SelectedIndex = Array.IndexOf(LanguageIds, "csharp");
        Editor.Text = SampleText.HelloWorld;
        Editor.Model.ContentChanged += (_, _) => RefreshStatus();
        // A load detects the indentation: show the tab size it chose.
        Editor.Model.OptionsChanged += (_, _) => TabSizeBox.Value = Editor.Model.Options.TabSize;
        Editor.SelectionChanged += (_, _) => RefreshStatus();
    }

    /// <summary>Refreshes the status line and starts the LSP demo and the self-checks.</summary>
    private void InitializeChecks()
    {
        RefreshStatus();
        InitializeLsp();
        InitializeDiffSelfCheck();
        InitializeTextSelfCheck();
        InitializeCommentsSelfCheck();
        InitializeHostApiSelfCheck();
    }

    /// <summary>Loads a feature sample into the editor; the host then focuses the editor.</summary>
    private void ShowFeatureSample(FeatureSample sample)
    {
        Editor.LanguageId = sample.LanguageId;
        LanguageBox.SelectedIndex = Array.IndexOf(LanguageIds, sample.LanguageId);
        Editor.Text = string.Empty;
        Editor.Text = sample.Text;
        Editor.SetSelections(new[] { Selection.Collapsed(new Position(sample.Line, sample.Column)) });
        FeatureHintText.Text = sample.Hint;
        OnFeatureSampleLoaded(sample);
    }
}
