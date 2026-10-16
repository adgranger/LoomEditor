// Ported from microsoft/vscode: src/vs/platform/keybinding/common/keybindingResolver.ts (_findCommand)
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

    /// <summary>Returns the id of the command to run for this key press, or <c>null</c> if no rule applies.</summary>
    public string? Resolve(Keybinding key, KeybindingContext context)
    {
        if (!_rulesByKey.TryGetValue(key, out List<KeybindingRule>? list))
        {
            return null;
        }

        KeybindingRule? best = null;
        foreach (KeybindingRule rule in list)
        {
            // ">=" keeps the last registered rule among equal weights.
            if ((rule.When is null || rule.When(context)) && (best is null || rule.Weight >= best.Weight))
            {
                best = rule;
            }
        }

        return best?.CommandId;
    }
}
