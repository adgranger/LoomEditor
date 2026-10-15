using LoomEditor.Core.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.Gallery;

/// <summary>The main window: one editor showing a sample file, and buttons that drive its text model.</summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Creates the window and loads the sample text.</summary>
    public MainWindow()
    {
        InitializeComponent();
        Editor.Text = SampleText.HelloWorld;
        Editor.Model.ContentChanged += (_, _) => RefreshStatus();
        RefreshStatus();
    }

    private void OnInsertHeader(object sender, RoutedEventArgs e) => ModelDemoActions.InsertHeader(Editor.Model);

    private void OnAppendLine(object sender, RoutedEventArgs e) => ModelDemoActions.AppendLine(Editor.Model);

    private void OnDeleteFirstLine(object sender, RoutedEventArgs e) => ModelDemoActions.DeleteFirstLine(Editor.Model);

    private void OnReplaceHello(object sender, RoutedEventArgs e) => ModelDemoActions.ReplaceHello(Editor.Model);

    private void OnUndo(object sender, RoutedEventArgs e) => Editor.Model.Undo();

    private void OnRedo(object sender, RoutedEventArgs e) => Editor.Model.Redo();

    private void OnLf(object sender, RoutedEventArgs e) => ModelDemoActions.SetEol(Editor.Model, EndOfLineSequence.LF);

    private void OnCrlf(object sender, RoutedEventArgs e) => ModelDemoActions.SetEol(Editor.Model, EndOfLineSequence.CRLF);

    private void OnReset(object sender, RoutedEventArgs e)
    {
        // The Text property does not change when the model was edited, so setting it to the same value again would be
        // ignored: go through an empty text first.
        Editor.Text = string.Empty;
        Editor.Text = SampleText.HelloWorld;
    }

    private void OnSmoothScrollingToggled(object sender, RoutedEventArgs e) => Editor.SmoothScrolling = SmoothScrollingSwitch.IsOn;

    private void OnFontSizeChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (Editor is not null && !double.IsNaN(args.NewValue))
        {
            Editor.FontSize = args.NewValue;
        }
    }

    private void OnFontFamilyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Editor is not null && FontFamilyBox.SelectedItem is ComboBoxItem { Content: string family })
        {
            Editor.FontFamily = new FontFamily(family);
        }
    }

    private void OnTabSizeChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (Editor is not null && !double.IsNaN(args.NewValue))
        {
            Editor.TabSize = (int)args.NewValue;
        }
    }

    private void RefreshStatus()
    {
        StatusText.Text = ModelDemoActions.FormatStatus(Editor.Model);
        UndoButton.IsEnabled = Editor.Model.CanUndo;
        RedoButton.IsEnabled = Editor.Model.CanRedo;
    }
}
