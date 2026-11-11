namespace StackQueueLab.UI;

public static class ConsoleInput
{
    public static string ReadLine(string prompt, bool allowEmpty = false)
    {
        while (true)
        {
            Console.WriteLine();
            ConsoleTheme.Color(ConsoleColor.DarkGray);
            Console.Write($"  {prompt} › ");
            ConsoleTheme.Color(ConsoleColor.Gray);

            var value = Console.ReadLine();

            if (value is null)
            {
                return string.Empty;
            }

            value = value.Trim();

            if (allowEmpty || value.Length > 0)
            {
                return value;
            }

            ConsoleTheme.Warning("Введите значение.");
        }
    }

    public static int ReadMenuChoice(int optionCount, bool allowBack = true)
    {
        while (true)
        {
            var value = ReadLine("Выбор");

            if (value.Equals("r", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("к", StringComparison.OrdinalIgnoreCase))
            {
                return -2;
            }

            if (value.Equals("b", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("н", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("back", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (int.TryParse(value, out var choice))
            {
                if (choice == 0 && allowBack)
                {
                    return 0;
                }

                if (choice >= 1 && choice <= optionCount)
                {
                    return choice;
                }
            }

            ConsoleTheme.Warning($"Введите число от 0 до {optionCount}.");
        }
    }

    public static int ReadInt(string prompt)
    {
        while (true)
        {
            var value = ReadLine(prompt);

            if (int.TryParse(value, out var number))
            {
                return number;
            }

            ConsoleTheme.Warning("Введите целое число.");
        }
    }

    public static bool Confirm(string question)    {
        while (true)
        {
            var answer = ReadLine(question + " (д/н)");

            if (answer.Equals("д", StringComparison.OrdinalIgnoreCase) ||
                answer.Equals("да", StringComparison.OrdinalIgnoreCase) ||
                answer.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                answer.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (answer.Equals("н", StringComparison.OrdinalIgnoreCase) ||
                answer.Equals("нет", StringComparison.OrdinalIgnoreCase) ||
                answer.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                answer.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            ConsoleTheme.Warning("Ответьте «д» или «н».");
        }
    }
}
