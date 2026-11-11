namespace StackQueueLab.UI;

/// <summary>
/// Аккуратный вывод целочисленного списка с выделением элементов,
/// которые реально изменили своё положение.
/// </summary>
public static class ListDisplay
{
    public static void Print(
        string title,
        IReadOnlyList<int> values,
        IReadOnlySet<int>? changedIndexes = null)
    {
        ConsoleTheme.Section(title);

        if (values.Count == 0)
        {
            ConsoleTheme.Muted("  [ ] — пусто");
            return;
        }

        Console.Write("  [ ");

        for (var i = 0; i < values.Count; i++)
        {
            var changed = changedIndexes?.Contains(i) == true;

            ConsoleTheme.Color(changed ? ConsoleColor.Cyan : ConsoleColor.Gray);
            Console.Write(values[i]);

            if (i + 1 < values.Count)
            {
                ConsoleTheme.Color(ConsoleColor.DarkGray);
                Console.Write(", ");
            }
        }

        ConsoleTheme.Color(ConsoleColor.Gray);
        Console.WriteLine(" ]");
    }
}
