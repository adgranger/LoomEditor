// Ported from microsoft/vscode: src/vs/editor/browser/coreCommands.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Commands;

// The scroll commands: they move the view, and only with revealCursor the cursors.
internal static partial class CoreNavigationCommands
{
    private sealed class EditorScrollImpl() : CoreEditorCommand<EditorScrollArguments>("editorScroll")
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, EditorScrollArguments args)
        {
            EditorScrollParsedArguments? parsed = LoomEditor.View.Commands.EditorScroll.Parse(args);
            if (parsed is null)
            {
                // illegal arguments
                return;
            }

            // Monaco: determineScrollMethod
            bool horizontal = parsed.Unit == EditorScrollUnit.Column
                && parsed.Direction is EditorScrollDirection.Left or EditorScrollDirection.Right;
            bool vertical = parsed.Unit != EditorScrollUnit.Column
                && parsed.Direction is EditorScrollDirection.Up or EditorScrollDirection.Down;

            if (horizontal)
            {
                RunHorizontalEditorScroll(viewModel, parsed);
            }
            else if (vertical)
            {
                RunVerticalEditorScroll(viewModel, args.Source, parsed);
            }

            // Otherwise: incompatible unit and direction
        }

        private static void RunVerticalEditorScroll(ViewModel viewModel, string? source, EditorScrollParsedArguments args)
        {
            double desiredScrollTop = ComputeDesiredScrollTop(viewModel, args);

            if (args.RevealCursor)
            {
                // must ensure cursor is in new visible range
                Range desiredVisibleViewRange = viewModel.GetCompletelyVisibleViewRangeAtScrollTop((int)desiredScrollTop);
                Range paddedRange = ViewModel.GetViewRangeWithCursorPadding(desiredVisibleViewRange);

                viewModel.SetCursorStates(
                    source,
                    CursorChangeReason.Explicit,
                    [CursorMoveCommands.FindPositionInViewportIfOutside(viewModel, viewModel.GetPrimaryCursorState(), paddedRange, args.Select)]);
            }

            viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(ScrollTop: desiredScrollTop), ScrollType.Smooth);
        }

        private static double ComputeDesiredScrollTop(ViewModel viewModel, EditorScrollParsedArguments args)
        {
            if (args.Unit == EditorScrollUnit.Line)
            {
                // scrolling by model lines
                Viewport futureViewport = viewModel.ViewLayout.GetFutureViewport();
                Range visibleViewRange = viewModel.GetCompletelyVisibleViewRangeAtScrollTop(futureViewport.Top);
                Range visibleModelRange = viewModel.CoordinatesConverter.ConvertViewRangeToModelRange(visibleViewRange);

                int desiredTopModelLineNumber = args.Direction == EditorScrollDirection.Up
                    ? Math.Max(1, visibleModelRange.StartLineNumber - args.Value) // must go x model lines up
                    : Math.Min(viewModel.Model.LineCount, visibleModelRange.StartLineNumber + args.Value); // must go x model lines down

                Position viewPosition = viewModel.CoordinatesConverter.ConvertModelPositionToViewPosition(new Position(desiredTopModelLineNumber, 1));
                return viewModel.ViewLayout.GetVerticalOffsetForLineNumber(viewPosition.LineNumber);
            }

            if (args.Unit == EditorScrollUnit.Editor)
            {
                // Monaco passes line number 0 for "top": a C# Position cannot hold 0, and the offset is for the line number only.
                int desiredTopModelLineNumber = 0;
                if (args.Direction == EditorScrollDirection.Down)
                {
                    desiredTopModelLineNumber = viewModel.Model.LineCount - viewModel.CursorConfig.PageSize;
                }

                return viewModel.ViewLayout.GetVerticalOffsetForLineNumber(desiredTopModelLineNumber);
            }

            int noOfLines;
            if (args.Unit == EditorScrollUnit.Page)
            {
                noOfLines = viewModel.CursorConfig.PageSize * args.Value;
            }
            else if (args.Unit == EditorScrollUnit.HalfPage)
            {
                // Math.round: .5 rounds up in JavaScript
                noOfLines = (int)Math.Floor((viewModel.CursorConfig.PageSize / 2.0) + 0.5) * args.Value;
            }
            else
            {
                noOfLines = args.Value;
            }

            int deltaLines = (args.Direction == EditorScrollDirection.Up ? -1 : 1) * noOfLines;
            return viewModel.ViewLayout.GetCurrentScrollTop() + (deltaLines * viewModel.CursorConfig.LineHeight);
        }

        private static void RunHorizontalEditorScroll(ViewModel viewModel, EditorScrollParsedArguments args)
        {
            int deltaColumns = (args.Direction == EditorScrollDirection.Left ? -1 : 1) * args.Value;
            double desiredScrollLeft = viewModel.ViewLayout.GetCurrentScrollLeft() + (deltaColumns * viewModel.CursorConfig.TypicalHalfwidthCharacterWidth);
            viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(ScrollLeft: desiredScrollLeft), ScrollType.Smooth);
        }
    }

    public static readonly CoreEditorCommand<EditorScrollArguments> EditorScroll = new EditorScrollImpl();

    private sealed class ScrollCommand(string id, string to, string by, int value) : CoreEditorCommand<BaseCommandArgs>(id)
    {
        public override void RunCoreEditorCommand(ViewModel viewModel, BaseCommandArgs args) =>
            EditorScroll.RunCoreEditorCommand(viewModel, new EditorScrollArguments(to, by, value, false, false, args.Source));
    }

    public static readonly CoreEditorCommand<BaseCommandArgs> ScrollLineUp = new ScrollCommand("scrollLineUp", "up", "wrappedLine", 1);

    public static readonly CoreEditorCommand<BaseCommandArgs> ScrollPageUp = new ScrollCommand("scrollPageUp", "up", "page", 1);

    public static readonly CoreEditorCommand<BaseCommandArgs> ScrollEditorTop = new ScrollCommand("scrollEditorTop", "up", "editor", 1);

    public static readonly CoreEditorCommand<BaseCommandArgs> ScrollLineDown = new ScrollCommand("scrollLineDown", "down", "wrappedLine", 1);

    public static readonly CoreEditorCommand<BaseCommandArgs> ScrollPageDown = new ScrollCommand("scrollPageDown", "down", "page", 1);

    public static readonly CoreEditorCommand<BaseCommandArgs> ScrollEditorBottom = new ScrollCommand("scrollEditorBottom", "down", "editor", 1);

    public static readonly CoreEditorCommand<BaseCommandArgs> ScrollLeft = new ScrollCommand("scrollLeft", "left", "column", 2);

    public static readonly CoreEditorCommand<BaseCommandArgs> ScrollRight = new ScrollCommand("scrollRight", "right", "column", 2);
}
