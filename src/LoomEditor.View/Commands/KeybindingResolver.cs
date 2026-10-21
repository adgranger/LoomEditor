// Ported from microsoft/vscode: src/vs/platform/keybinding/common/keybindingResolver.ts (resolve, _findCommand)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Commands;

/// <summary>Turns a key press and the editor state into a command id.</summary>
/// <remarks>
/// <para>
/// Monaco sorts the rules by weight (stable, so registration order breaks ties) and searches from the end: the heaviest
/// matching rule wins, and among equal weights the one registered last. This class does the same without sorting.
/// </para>
/// <para>Threading: immutable; thread-safe as long as the rules' conditions are.</para>
/// </remarks>
internal sealed class KeybindingResolver
{
    private readonly Dictionary<Keybinding, List<KeybindingRule>> _rulesByKey = [];

    /// <summary>Creates a resolver over <paramref name="rules"/>, in registration order.</summary>
    public KeybindingResolver(IEnumerable<KeybindingRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        foreach (KeybindingRule rule in rules)
        {
            if (!_rulesByKey.TryGetValue(rule.Keybinding, out List<KeybindingRule>? list))
            {
                list = [];
                _rulesByKey.Add(rule.Keybinding, list);
            }

            list.Add(rule);
        }
    }

    /// <summary>A resolver over <see cref="DefaultKeybindings.Windows"/>.</summary>
    public static KeybindingResolver Default { get; } = new(DefaultKeybindings.Windows);

    /// <summary>Returns the id of the command to run for this key press, or <c>null</c> if no rule applies (or the key press starts a chord).</summary>
    public string? Resolve(Keybinding key, KeybindingContext context)
    {
        KeybindingResolution resolution = Resolve(context, null, key);
        return resolution.Kind == KeybindingResolutionKind.KbFound ? resolution.CommandId : null;
    }

    /// <summary>
    /// Resolves <paramref name="keypress"/>, which may be the second key press of a chord (Monaco: <c>resolve</c>; two key
    /// presses at most).
    /// </summary>
    /// <param name="context">The editor state the rules' conditions read.</param>
    /// <param name="currentChord">The first key press of the chord in progress, or <see langword="null"/>.</param>
    /// <param name="keypress">The key press to resolve.</param>
    public KeybindingResolution Resolve(KeybindingContext context, Keybinding? currentChord, Keybinding keypress)
    {
        if (!_rulesByKey.TryGetValue(currentChord ?? keypress, out List<KeybindingRule>? list))
        {
            return new KeybindingResolution(KeybindingResolutionKind.NoMatchingKb);
        }

        KeybindingRule? best = null;
        foreach (KeybindingRule rule in list)
        {
            // With a chord in progress, only the rules whose second key press is this one apply.
            if (currentChord is not null && rule.Chord != keypress)
            {
                continue;
            }

            // ">=" keeps the last registered rule among equal weights.
            if ((rule.When is null || rule.When(context)) && (best is null || rule.Weight >= best.Weight))
            {
                best = rule;
            }
        }

        if (best is null)
        {
            return new KeybindingResolution(KeybindingResolutionKind.NoMatchingKb);
        }

        return best.Chord is not null && currentChord is null
            ? new KeybindingResolution(KeybindingResolutionKind.MoreChordsNeeded)
            : new KeybindingResolution(KeybindingResolutionKind.KbFound, best.CommandId);
    }
}
