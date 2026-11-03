#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

// Applying the text, language and theme properties to the two editors.
public sealed partial class LoomDiffEditor
{
    private void ApplyText(LoomEditor? editor, string? text)
    {
        // Before the template is applied OnApplyTemplate sets the texts.
        if (_isSyncingText || editor is null)
        {
            return;
        }

        _isSyncingText = true;
        try
        {
            editor.ResetText(text ?? string.Empty);
        }
        finally
        {
            _isSyncingText = false;
        }
    }

    private void ApplyLanguageAndTheme()
    {
        foreach (LoomEditor? editor in new[] { _original, _modified })
        {
            if (editor is not null)
            {
                editor.LanguageId = LanguageId;
                editor.Theme = Theme;
            }
        }

        InvalidateOverview();
    }
}
