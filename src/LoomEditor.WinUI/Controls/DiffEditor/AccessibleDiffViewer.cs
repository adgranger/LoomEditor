// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/accessibleDiffViewer.ts (View)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Themes;
using LoomEditor.View.Accessibility;
using LoomEditor.View.DiffEditor;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace LoomEditor.WinUI.Controls.DiffEditor;

/// <summary>
/// The accessible diff viewer: a list that shows one group of changes as rows, covering both editors (Monaco: <c>View</c> of
/// <c>accessibleDiffViewer.ts</c>). Code only, no bindings: the rows are <see cref="ListViewItem"/>s built from <see cref="DiffViewerRowItem"/>s.
/// </summary>
/// <remarks>
/// Behavior differences: the lines are plain text without any syntax color (like Monaco), in the font of the modified editor; the
/// arrows are handled before the <see cref="ListView"/> sees them, the model decides and the list follows its <c>Changed</c> event.
/// Threading: UI thread only.
/// </remarks>
internal sealed partial class AccessibleDiffViewer : UserControl
{
    private const double NumberColumnWidth = 48;
    private const double SignColumnWidth = 24;

    private readonly AccessibleDiffViewerModel _model;
    private readonly IAccessibleDiffViewerHost _host;
    private readonly EditorTheme _theme;
    private readonly FontFamily _fontFamily;
    private readonly double _fontSize;
    private readonly int _tabSize;
    private readonly ListView _list = new() { SelectionMode = ListViewSelectionMode.Single, IsItemClickEnabled = true, IsTabStop = false };
    private readonly List<ListViewItem> _items = [];
    private IReadOnlyList<ViewElementGroup>? _renderedGroups;
    private int _renderedGroupIndex = -1;
    private bool _isSyncing;

    /// <summary>Creates the viewer over a model and shows its current group.</summary>
    /// <param name="model">The model; the viewer follows its <c>Changed</c> event.</param>
    /// <param name="host">The editors, for the text of the lines.</param>
    /// <param name="theme">The colors.</param>
    /// <param name="fontFamily">The font of the modified editor.</param>
    /// <param name="fontSize">The size of the font of the modified editor.</param>
    /// <param name="tabSize">The tab size of the modified editor.</param>
    public AccessibleDiffViewer(AccessibleDiffViewerModel model, IAccessibleDiffViewerHost host, EditorTheme theme, FontFamily fontFamily, double fontSize, int tabSize)
    {
        _model = model;
        _host = host;
        _theme = theme;
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        _tabSize = tabSize;

        IsTabStop = false;
        var close = new Button { Content = "✕", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(4) };
        AutomationProperties.SetName(close, "Close");
        close.Click += (_, _) => _model.Close();

        AutomationProperties.SetName(_list, AccessibilityMessages.DiffViewerListLabel);
        _list.SelectionChanged += OnSelectionChanged;

        var grid = new Grid { Background = Brush(theme.GetColor("editor.background")) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(close);
        Grid.SetRow(_list, 1);
        grid.Children.Add(_list);
        Content = grid;

        PreviewKeyDown += OnPreviewKeyDown;
        _model.Changed += OnModelChanged;
        Render();
    }

    /// <summary>Stops following the model.</summary>
    public void Detach() => _model.Changed -= OnModelChanged;

    private void OnModelChanged(object? sender, EventArgs e) => Render();

    // Rebuilds the rows when the group changed (Monaco: the "render" autorun reads the current group), then selects the current row.
    private void Render()
    {
        _isSyncing = true;
        try
        {
            if (!ReferenceEquals(_renderedGroups, _model.Groups) || _renderedGroupIndex != _model.CurrentGroupIndex)
            {
                RebuildRows();
            }

            int index = _model.CurrentElementIndex;
            if (index >= 0 && index < _items.Count)
            {
                _list.SelectedIndex = index;
                _list.ScrollIntoView(_items[index]);
                _items[index].Focus(FocusState.Keyboard);
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void RebuildRows()
    {
        _renderedGroups = _model.Groups;
        _renderedGroupIndex = _model.CurrentGroupIndex;
        _items.Clear();
        _list.Items.Clear();
        if (_model.CurrentGroup is not { } group)
        {
            return;
        }

        foreach (ViewElement element in group.Lines)
        {
            DiffViewerRowItem row = DiffViewerRowItem.Create(element, group, _model.CurrentGroupIndex, _model.Groups.Count, _host, _tabSize);
            var item = new ListViewItem { Content = CreateRow(row), Padding = new Thickness(0), MinHeight = 0, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            AutomationProperties.SetName(item, row.AutomationName);
            item.GotFocus += (_, _) =>
            {
                if (!_isSyncing)
                {
                    _model.GoToLine(element);
                }
            };
            _items.Add(item);
            _list.Items.Add(item);
        }
    }

    // Monaco: _createRow. The classes line-insert / line-delete are the background of the row, char-insert / char-delete the one of the numbers.
    private Grid CreateRow(DiffViewerRowItem row)
    {
        LineType type = row.Element.Type;
        var grid = new Grid
        {
            Background = type switch
            {
                LineType.Added => Brush(_theme.GetColor("diffEditor.insertedLineBackground")),
                LineType.Deleted => Brush(_theme.GetColor("diffEditor.removedLineBackground")),
                _ => null,
            },
        };

        if (type == LineType.Header)
        {
            grid.Children.Add(Text(row.Text, bold: true));
            return grid;
        }

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NumberColumnWidth) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NumberColumnWidth) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(SignColumnWidth) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Brush? numberBackground = type switch
        {
            LineType.Added => Brush(_theme.GetColor("diffEditor.insertedTextBackground")),
            LineType.Deleted => Brush(_theme.GetColor("diffEditor.removedTextBackground")),
            _ => null,
        };
        Brush? numberForeground = Brush(_theme.GetColor("editorLineNumber.foreground"));
        AddCell(grid, 0, row.OriginalNumber, numberForeground, numberBackground, HorizontalAlignment.Right);
        AddCell(grid, 1, row.ModifiedNumber, numberForeground, numberBackground, HorizontalAlignment.Right);
        AddCell(grid, 2, row.Sign, Brush(_theme.GetColor("editor.foreground")), null, HorizontalAlignment.Center);
        AddCell(grid, 3, row.Text, Brush(_theme.GetColor("editor.foreground")), null, HorizontalAlignment.Left);
        return grid;
    }

    private void AddCell(Grid grid, int column, string text, Brush? foreground, Brush? background, HorizontalAlignment alignment)
    {
        var cell = new Border { Background = background, Child = Text(text, foreground: foreground, alignment: alignment) };
        Grid.SetColumn(cell, column);
        grid.Children.Add(cell);
    }

    private TextBlock Text(string text, Brush? foreground = null, bool bold = false, HorizontalAlignment alignment = HorizontalAlignment.Left) =>
        new()
        {
            Text = text,
            FontFamily = _fontFamily,
            FontSize = _fontSize,
            FontWeight = bold ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
            Foreground = foreground ?? Brush(_theme.GetColor("editor.foreground")),
            IsTextSelectionEnabled = false,
            HorizontalAlignment = alignment,
            Margin = new Thickness(4, 0, 4, 0),
        };

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isSyncing && _list.SelectedIndex is >= 0 and var index && _model.CurrentGroup is { } group && index < group.Lines.Count)
        {
            _model.GoToLine(group.Lines[index]);
        }
    }

    // Monaco: the keydown listener of the view; Ctrl and Alt variants of the arrows, any modifier of Escape.
    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        bool ctrl = IsDown(VirtualKey.Control);
        bool alt = IsDown(VirtualKey.Menu);
        bool shift = IsDown(VirtualKey.Shift);
        bool arrowModifiers = !shift && !(ctrl && alt);
        bool plain = !ctrl && !alt && !shift;

        switch (e.Key)
        {
            case VirtualKey.Down when arrowModifiers:
                _model.GoToNextLine();
                break;
            case VirtualKey.Up when arrowModifiers:
                _model.GoToPreviousLine();
                break;
            case VirtualKey.Escape:
                _model.Close();
                break;
            case VirtualKey.Space or VirtualKey.Enter when plain:
                _model.RevealCurrentElementInEditor();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private static bool IsDown(VirtualKey key) =>
        Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

    private static SolidColorBrush? Brush(ThemeColor? color) =>
        color is { } c ? new SolidColorBrush(Windows.UI.Color.FromArgb(c.A, c.R, c.G, c.B)) : null;
}
