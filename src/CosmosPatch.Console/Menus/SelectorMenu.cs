using CosmosPatch.Domain.Interfaces;

namespace CosmosPatch.Console.Menus;

/// <summary>
/// Arrow-key driven console menu for selecting a single item from a list.
/// Protocol: ↑/↓ to navigate, Enter to confirm, T for text input, Esc to exit.
/// </summary>
public static class SelectorMenu
{
    /// <summary>Presents a scrollable list menu and returns the user's selection.</summary>
    public static string SelectItem(string title, List<string> items, IAppLogger? logger = null)
    {
        if (items is null || items.Count == 0)
            throw new ArgumentException("No items available for selection.");

        if (items.Count == 1)
        {
            logger?.WriteMessageOnConsole($"Auto-selected: {items[0]}");
            System.Console.WriteLine("Press any key to continue...");
            System.Console.ReadKey();
            return items[0];
        }

        int selectedIndex = 0;
        System.Console.CursorVisible = false;

        try
        {
            while (true)
            {
                System.Console.Clear();
                System.Console.WriteLine($"=== {title} ===");
                System.Console.WriteLine("  ↑/↓  Navigate    Enter  Confirm    T  Text input    Esc  Exit\n");

                for (int i = 0; i < items.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        System.Console.BackgroundColor = ConsoleColor.Gray;
                        System.Console.ForegroundColor = ConsoleColor.Black;
                        System.Console.WriteLine($" → {items[i]} ");
                        System.Console.ResetColor();
                    }
                    else
                    {
                        System.Console.WriteLine($"   {items[i]}");
                    }
                }
                System.Console.WriteLine($"\n  Total: {items.Count}");

                ConsoleKeyInfo keyInfo = System.Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = Math.Max(0, selectedIndex - 1);
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = Math.Min(items.Count - 1, selectedIndex + 1);
                        break;
                    case ConsoleKey.Enter:
                        System.Console.CursorVisible = true;
                        return items[selectedIndex];
                    case ConsoleKey.T:
                        System.Console.CursorVisible = true;
                        return SelectItemByText(title, items, logger);
                    case ConsoleKey.Escape:
                        System.Console.CursorVisible = true;
                        System.Console.WriteLine("\nExiting...");
                        Environment.Exit(0);
                        break;
                }
            }
        }
        finally
        {
            System.Console.CursorVisible = true;
        }
    }

    private static string SelectItemByText(string title, List<string> items, IAppLogger? logger)
    {
        System.Console.Clear();
        System.Console.WriteLine($"=== {title} — Text Input ===");
        System.Console.WriteLine("Enter a number (index) or partial/full name:\n");
        for (int i = 0; i < items.Count; i++)
            System.Console.WriteLine($"  {i + 1}. {items[i]}");

        while (true)
        {
            System.Console.Write("\nYour input: ");
            string? input = System.Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(input)) continue;

            // Try by number
            if (int.TryParse(input, out int idx) && idx >= 1 && idx <= items.Count)
                return items[idx - 1];

            // Try by exact match
            string? exact = items.FirstOrDefault(x => x.Equals(input, StringComparison.OrdinalIgnoreCase));
            if (exact is not null) return exact;

            // Try by partial match
            List<string> partial = items.Where(x => x.Contains(input, StringComparison.OrdinalIgnoreCase)).ToList();
            if (partial.Count == 1)
            {
                System.Console.WriteLine($"Matched: {partial[0]}");
                return partial[0];
            }
            if (partial.Count > 1)
            {
                System.Console.WriteLine("Multiple matches found:");
                partial.ForEach(x => System.Console.WriteLine($"  {x}"));
                continue;
            }

            System.Console.WriteLine($"No match found for '{input}'. Try again.");
        }
    }
}
