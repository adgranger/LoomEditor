namespace LoomEditor.Gallery;

/// <summary>
/// The two texts compared by the diff editor demo. No WinUI types, so the tests can compile this file.
/// </summary>
/// <remarks>
/// Built so that every feature of the diff editor has something to show:
/// <list type="bullet">
/// <item>an unchanged block of more than 20 lines at the top and of more than 25 lines at the end (hidden unchanged regions);</item>
/// <item><c>Describe</c> moves from the top to the end of the class (a deletion and an insertion: moves are not drawn);</item>
/// <item>one line of <c>Add</c> only changes its indentation (visible with <c>IgnoreTrimWhitespace</c> off);</item>
/// <item><c>Remove</c> is rewritten (a replacement with inner character changes);</item>
/// <item><c>Clear</c> is deleted, <c>GetLowStock</c> is added (a pure deletion and a pure insertion);</item>
/// <item>one word of a long line is renamed (a character level highlight).</item>
/// </list>
/// Threading: immutable.
/// </remarks>
internal static class DiffSamples
{
    /// <summary>The original side.</summary>
    public static string Original { get; } = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        namespace Demo.Inventory;

        /// <summary>Keeps track of the items in stock.</summary>
        public sealed class Warehouse
        {
            private readonly Dictionary<string, int> _stock = new();
            private readonly List<string> _history = new();

            /// <summary>The name of the warehouse.</summary>
            public string Name { get; }

            /// <summary>The number of different items in stock.</summary>
            public int ItemCount => _stock.Count;

            /// <summary>Creates an empty warehouse.</summary>
            public Warehouse(string name)
            {
                Name = name;
            }

            /// <summary>Returns the quantity of an item, 0 when it is unknown.</summary>
            public int GetQuantity(string item)
            {
                return _stock.TryGetValue(item, out int quantity) ? quantity : 0;
            }

            /// <summary>Describes the warehouse in one line.</summary>
            public string Describe()
            {
                return $"{Name}: {ItemCount} item(s)";
            }

            /// <summary>Adds items to the stock.</summary>
            public void Add(string item, int quantity)
            {
                if (quantity <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(quantity));
                }

                _stock[item] = GetQuantity(item) + quantity;
                _history.Add($"add {item} {quantity}");
            }

            /// <summary>Removes items from the stock.</summary>
            public bool Remove(string item, int quantity)
            {
                int current = GetQuantity(item);
                if (current < quantity)
                {
                    return false;
                }

                _stock[item] = current - quantity;
                return true;
            }

            /// <summary>Removes every item.</summary>
            public void Clear()
            {
                _stock.Clear();
                _history.Clear();
            }

            private const string LowStockMessage = "The quantity of {0} is below the minimum of {1} units, please reorder soon.";

            /// <summary>Tells whether an item must be ordered again.</summary>
            public string? CheckLowStock(string item, int minimum)
            {
                return GetQuantity(item) < minimum ? string.Format(LowStockMessage, item, minimum) : null;
            }

            /// <summary>The operations done so far, oldest first.</summary>
            public IReadOnlyList<string> History => _history;

            /// <summary>A copy of the stock at a given time.</summary>
            public IReadOnlyDictionary<string, int> Snapshot()
            {
                return new Dictionary<string, int>(_stock);
            }

            /// <summary>Writes the stock, one item per line, sorted by name.</summary>
            public string Report()
            {
                var lines = new List<string>();
                foreach (KeyValuePair<string, int> pair in _stock.OrderBy(p => p.Key))
                {
                    lines.Add($"{pair.Key}: {pair.Value}");
                }

                return string.Join(Environment.NewLine, lines);
            }

            /// <summary>Tells whether the warehouse has nothing in stock.</summary>
            public bool IsEmpty => _stock.Count == 0;
        }
        """.ReplaceLineEndings("\n");

    /// <summary>The modified side.</summary>
    public static string Modified { get; } = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        namespace Demo.Inventory;

        /// <summary>Keeps track of the items in stock.</summary>
        public sealed class Warehouse
        {
            private readonly Dictionary<string, int> _stock = new();
            private readonly List<string> _history = new();

            /// <summary>The name of the warehouse.</summary>
            public string Name { get; }

            /// <summary>The number of different items in stock.</summary>
            public int ItemCount => _stock.Count;

            /// <summary>Creates an empty warehouse.</summary>
            public Warehouse(string name)
            {
                Name = name;
            }

            /// <summary>Returns the quantity of an item, 0 when it is unknown.</summary>
            public int GetQuantity(string item)
            {
                return _stock.TryGetValue(item, out int quantity) ? quantity : 0;
            }

            /// <summary>Adds items to the stock.</summary>
            public void Add(string item, int quantity)
            {
                if (quantity <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(quantity));
                }

                _stock[item] = GetQuantity(item) + quantity;
                    _history.Add($"add {item} {quantity}");
            }

            /// <summary>Removes items from the stock, never going below zero.</summary>
            public bool Remove(string item, int quantity)
            {
                int current = GetQuantity(item);
                if (current < quantity)
                {
                    _history.Add($"refused {item} {quantity}");
                    return false;
                }

                _stock[item] = Math.Max(0, current - quantity);
                _history.Add($"remove {item} {quantity}");
                return true;
            }

            /// <summary>Returns the items that are below a quantity, sorted by name.</summary>
            public IReadOnlyList<string> GetLowStock(int threshold)
            {
                return _stock.Where(p => p.Value < threshold).Select(p => p.Key).OrderBy(name => name).ToList();
            }

            private const string LowStockMessage = "The quantity of {0} is below the minimum of {1} units, please restock soon.";

            /// <summary>Tells whether an item must be ordered again.</summary>
            public string? CheckLowStock(string item, int minimum)
            {
                return GetQuantity(item) < minimum ? string.Format(LowStockMessage, item, minimum) : null;
            }

            /// <summary>The operations done so far, oldest first.</summary>
            public IReadOnlyList<string> History => _history;

            /// <summary>A copy of the stock at a given time.</summary>
            public IReadOnlyDictionary<string, int> Snapshot()
            {
                return new Dictionary<string, int>(_stock);
            }

            /// <summary>Writes the stock, one item per line, sorted by name.</summary>
            public string Report()
            {
                var lines = new List<string>();
                foreach (KeyValuePair<string, int> pair in _stock.OrderBy(p => p.Key))
                {
                    lines.Add($"{pair.Key}: {pair.Value}");
                }

                return string.Join(Environment.NewLine, lines);
            }

            /// <summary>Tells whether the warehouse has nothing in stock.</summary>
            public bool IsEmpty => _stock.Count == 0;

            /// <summary>Describes the warehouse in one line.</summary>
            public string Describe()
            {
                return $"{Name}: {ItemCount} item(s)";
            }
        }
        """.ReplaceLineEndings("\n");

    /// <summary>
    /// Two texts of 5 000 generated lines where about one line in a hundred differs, from a fixed seed (the "Large files" button).
    /// </summary>
    public static (string Original, string Modified) CreateLargePair()
    {
        const int LineCount = 5000;
        var random = new Random(7);
        var original = new List<string>(LineCount);
        var modified = new List<string>(LineCount);
        for (int i = 1; i <= LineCount; i++)
        {
            string line = $"    var value{i} = Compute({i % 97}, {i * 31 % 1013}); // generated line {i}";
            original.Add(line);
            modified.Add(random.Next(100) == 0 ? line.Replace("Compute", "Evaluate", StringComparison.Ordinal) + " // changed" : line);
        }

        return (string.Join('\n', original), string.Join('\n', modified));
    }
}
