// Ported from microsoft/vscode: src/vs/platform/keybinding/common/keybindingResolver.ts (ResultKind, ResolutionResult)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Commands;

/// <summary>What <see cref="KeybindingResolver.Resolve(KeybindingContext, Keybinding?, Keybinding)"/> found for a key press (Monaco: <c>ResultKind</c>).</summary>
/// <remarks>Threading: an enum; thread-safe.</remarks>
internal enum KeybindingResolutionKind
{
    /// <summary>No keybinding matches.</summary>
    NoMatchingKb,

    /// <summary>The key press starts a chord: another key press is needed.</summary>
    MoreChordsNeeded,

    /// <summary>A keybinding matches: <see cref="KeybindingResolution.CommandId"/> is its command.</summary>
    KbFound,
}

/// <summary>The result of resolving a key press (Monaco: <c>ResolutionResult</c>).</summary>
/// <param name="Kind">What was found.</param>
/// <param name="CommandId">The command to run, for <see cref="KeybindingResolutionKind.KbFound"/>.</param>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal readonly record struct KeybindingResolution(KeybindingResolutionKind Kind, string? CommandId = null);
