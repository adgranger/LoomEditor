using LoomEditor.TextMate.Grammars;
using LoomEditor.Themes;

namespace LoomEditor.Gallery;

public sealed partial class MainWindow
{
    /// <summary>The ids of the built-in languages (the items of the language picker).</summary>
    public string[] LanguageIds { get; } = [.. BuiltInLanguages.Ids];

    /// <summary>The names of the built-in themes (the items of the theme picker).</summary>
    public string[] ThemeNames { get; } = [.. EditorTheme.BuiltIn.Select(theme => theme.Name)];

    // Items are added in code (strings as ComboBoxItem content): the pickers are filled from the library's lists.
    private void PopulatePickers()
    {
        foreach (string id in LanguageIds)
        {
            LanguageBox.Items.Add(new ComboBoxItem { Content = id });
        }

        foreach (string name in ThemeNames)
        {
            ThemeBox.Items.Add(new ComboBoxItem { Content = name });
        }

        foreach (FeatureSample sample in FeatureSamples.All)
        {
            FeatureSampleBox.Items.Add(new ComboBoxItem { Content = sample.Name });
        }

        LanguageBox.SelectedIndex = 0;
        ThemeBox.SelectedIndex = 0;
    }
}
