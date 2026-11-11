using StackQueueLab.Services;

namespace StackQueueLab.UI.Controllers;

public sealed class LinkedListLabController
{
    private static readonly string[] TaskNames =
    [
        "1. Переворот списка",
        "2. Перенос крайнего элемента",
        "3. Количество различных элементов",
        "4. Удаление неуникальных элементов",
        "5. Вставить список после первого x",
        "6. Вставить E в упорядоченный список",
        "7. Удалить все элементы e",
        "8. Вставить F перед первым E"
    ];

    public void Run()
    {
        while (true)
        {
            ConsoleTheme.BeginScreen("Главное меню / Алгоритмы связного списка");
            ConsoleTheme.Title(
                "Алгоритмы связного списка",
                "Каждое задание выполняет CustomLinkedList");

            for (var i = 0; i < TaskNames.Length; i++)
            {
                ConsoleTheme.MenuOption($"{i + 1}", TaskNames[i]);
            }

            ConsoleTheme.MenuOption("0", "Назад");
            ConsoleTheme.Rule();

            var choice = ConsoleInput.ReadMenuChoice(TaskNames.Length);

            if (choice == 0)
            {
                return;
            }

            RunTask(choice);
            ConsoleTheme.Pause("Enter — назад в меню алгоритмов");
        }
    }

    private void RunTask(int taskNumber)
    {
        ConsoleTheme.BeginScreen("Главное меню / Алгоритмы связного списка");
        ConsoleTheme.Title(TaskNames[taskNumber - 1]);

        if (!TryChooseValues(taskNumber, out var values))
        {
            return;
        }

        var variant = 0;
        var firstParameter = 0;
        var secondParameter = 0;

        if (taskNumber == 2)
        {
            var selectedVariant = ChooseMoveVariant();

            if (selectedVariant is null)
            {
                return;
            }

            variant = selectedVariant.Value;
        }

        if (taskNumber == 5)
        {
            firstParameter = ConsoleInput.ReadInt("Введите x");
        }

        if (taskNumber == 6)
        {
            firstParameter = ConsoleInput.ReadInt("Введите E");

            if (!LinkedListTaskService.IsSortedNondecreasing(values))
            {
                ConsoleTheme.Warning("Введённый список не является неубывающим.");

                if (ConsoleInput.Confirm("Автоматически упорядочить его перед вставкой?"))
                {
                    values = LinkedListTaskService.SortNondecreasing(values);
                    ConsoleTheme.Success("Список упорядочен.");
                }
                else
                {
                    ConsoleTheme.Muted("Задание рассчитано на упорядоченный список.");
                }
            }
        }

        if (taskNumber == 7)
        {
            firstParameter = ConsoleInput.ReadInt("Введите e");
        }

        if (taskNumber == 8)
        {
            firstParameter = ConsoleInput.ReadInt("Введите F");
            secondParameter = ConsoleInput.ReadInt("Введите E");
        }

        var result = LinkedListTaskService.Execute(
            taskNumber,
            values,
            variant,
            firstParameter,
            secondParameter);

        ListDisplay.Print("Исходный список", result.Before);
        ListDisplay.Print(
            "Список после выполнения",
            result.After,
            result.ChangedIndexes);

        if (!string.IsNullOrWhiteSpace(result.Note))
        {
            ConsoleTheme.KeyValue("Результат", result.Note);
        }

        ConsoleTheme.Muted(
            "  Элементы, изменившие положение, выделены голубым. Сдвиги из-за");
        ConsoleTheme.Muted("  чужих вставок и удалений цветом не выделяются.");
    }

    private static bool TryChooseValues(
        int taskNumber,
        out IReadOnlyList<int> values)
    {
        ConsoleTheme.Section("Источник данных");
        ConsoleTheme.MenuOption("1", "Случайные значения", "7–25 чисел от 0 до 999");
        ConsoleTheme.MenuOption("2", "Ручной ввод", "числа через пробел");
        ConsoleTheme.MenuOption("0", "Назад");

        var choice = ConsoleInput.ReadMenuChoice(2);

        if (choice == 0 || choice == -2)
        {
            values = Array.Empty<int>();
            return false;
        }

        if (choice == 1)
        {
            values = LinkedListTaskService.GenerateRandom(sorted: taskNumber == 6);
            return true;
        }

        while (true)
        {
            var input = ConsoleInput.ReadLine("Список", allowEmpty: true);

            if (LinkedListTaskService.TryParseManual(input, out values))
            {
                return true;
            }

            ConsoleTheme.Warning("Нужны целые числа, разделённые пробелами или запятыми.");
        }
    }

    private static int? ChooseMoveVariant()
    {
        ConsoleTheme.Section("Вариант переноса");
        ConsoleTheme.MenuOption("1", "Последний элемент в начало", "сдвинутые элементы не выделяются");
        ConsoleTheme.MenuOption("2", "Первый элемент в конец", "сдвинутые элементы не выделяются");
        ConsoleTheme.MenuOption("0", "Назад");

        var choice = ConsoleInput.ReadMenuChoice(2);

        if (choice == 0 || choice == -2)
        {
            return null;
        }

        return choice == 1 ? 1 : 0;
    }
}
