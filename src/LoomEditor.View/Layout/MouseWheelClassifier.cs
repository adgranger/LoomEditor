// Ported from microsoft/vscode: src/vs/base/browser/ui/scrollbar/scrollableElement.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// Tells a physical mouse wheel (smooth scrolling) from a touchpad or a magic mouse (immediate scrolling) by scoring
/// the last wheel events. Monaco: <c>MouseWheelClassifier</c>.
/// </summary>
/// <remarks>
/// Threading: UI thread only. Behavior difference: Monaco has one global <c>INSTANCE</c>; each editor owns its own
/// classifier here. <c>acceptStandardWheelEvent</c> (it reads a DOM event) is not ported: call <see cref="Accept"/>.
/// </remarks>
internal sealed class MouseWheelClassifier
{
    private const int Capacity = 5;

    // JavaScript's Number.EPSILON (2^-52); double.Epsilon in .NET is the smallest denormal.
    private const double JsNumberEpsilon = 2.220446049250313e-16;

    private readonly Item?[] _memory = new Item?[Capacity];
    private int _front = -1;
    private int _rear = -1;

    /// <summary>Whether the recent events look like they come from a physical wheel.</summary>
    public bool IsPhysicalMouseWheel()
    {
        if (_front == -1 && _rear == -1)
        {
            // no elements
            return false;
        }

        // 0.5 * last + 0.25 * 2nd last + 0.125 * 3rd last + ...
        double remainingInfluence = 1;
        double score = 0;
        int iteration = 1;

        int index = _rear;
        while (true)
        {
            double influence = index == _front ? remainingInfluence : Math.Pow(2, -iteration);
            remainingInfluence -= influence;
            score += _memory[index]!.Score * influence;

            if (index == _front)
            {
                break;
            }

            index = (Capacity + index - 1) % Capacity;
            iteration++;
        }

        return score <= 0.5;
    }

    /// <summary>Records a wheel event. <paramref name="timestamp"/> is in milliseconds.</summary>
    public void Accept(double timestamp, double deltaX, double deltaY)
    {
        Item? previousItem = null;
        var item = new Item(timestamp, deltaX, deltaY);

        if (_front == -1 && _rear == -1)
        {
            _memory[0] = item;
            _front = 0;
            _rear = 0;
        }
        else
        {
            previousItem = _memory[_rear];

            _rear = (_rear + 1) % Capacity;
            if (_rear == _front)
            {
                // Drop oldest
                _front = (_front + 1) % Capacity;
            }

            _memory[_rear] = item;
        }

        item.Score = ComputeScore(item, previousItem);
    }

    /// <summary>
    /// A score between 0 and 1 for <paramref name="item"/>: towards 0 the source looks like a physical wheel, towards 1
    /// like a touchpad or a magic mouse.
    /// </summary>
    private static double ComputeScore(Item item, Item? previousItem)
    {
        if (Math.Abs(item.DeltaX) > 0 && Math.Abs(item.DeltaY) > 0)
        {
            // both axes exercised => definitely not a physical mouse wheel
            return 1;
        }

        double score = 0.5;

        if (!IsAlmostInt(item.DeltaX) || !IsAlmostInt(item.DeltaY))
        {
            // non-integer deltas => indicator that this is not a physical mouse wheel
            score += 0.25;
        }

        // Non-accelerating scroll => indicator that this is a physical mouse wheel.
        // These can be identified by seeing whether they are the module of one another.
        if (previousItem is not null)
        {
            double absDeltaX = Math.Abs(item.DeltaX);
            double absDeltaY = Math.Abs(item.DeltaY);

            double absPreviousDeltaX = Math.Abs(previousItem.DeltaX);
            double absPreviousDeltaY = Math.Abs(previousItem.DeltaY);

            // Min 1 to avoid division by zero, module 1 will still be 0.
            double minDeltaX = Math.Max(Math.Min(absDeltaX, absPreviousDeltaX), 1);
            double minDeltaY = Math.Max(Math.Min(absDeltaY, absPreviousDeltaY), 1);

            double maxDeltaX = Math.Max(absDeltaX, absPreviousDeltaX);
            double maxDeltaY = Math.Max(absDeltaY, absPreviousDeltaY);

            bool isSameModulo = maxDeltaX % minDeltaX == 0 && maxDeltaY % minDeltaY == 0;
            if (isSameModulo)
            {
                score -= 0.5;
            }
        }

        return Math.Min(Math.Max(score, 0), 1);
    }

    private static bool IsAlmostInt(double value)
    {
        double epsilon = JsNumberEpsilon * 100; // small tolerance for floating-point errors
        double delta = Math.Abs(Math.Round(value, MidpointRounding.AwayFromZero) - value);
        return delta < 0.01 + epsilon;
    }

    private sealed class Item
    {
        public Item(double timestamp, double deltaX, double deltaY)
        {
            Timestamp = timestamp;
            DeltaX = deltaX;
            DeltaY = deltaY;
        }

        public double Timestamp { get; }

        public double DeltaX { get; }

        public double DeltaY { get; }

        public double Score { get; set; }
    }
}
