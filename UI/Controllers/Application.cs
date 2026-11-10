using StackQueueLab.Services;
using StackQueueLab.UI;

namespace StackQueueLab.UI.Controllers;

public sealed class Application
{
    private readonly ProjectPaths _paths;
    private readonly StackLabController _stackLab;
    private readonly QueueLabController _queueLab;
    private readonly PostfixLabController _postfixLab;
    private readonly InfixLabController _infixLab;

    public Application()
    {
        _paths = ProjectPaths.Create();
        var runner = new OperationSetRunner();
        var reports = new CsvReportWriter(_paths);
        var picker = new FilePicker();
        var expressions = new ExpressionFileService();

        _stackLab = new StackLabController(_paths, picker, runner, reports);
        _queueLab = new QueueLabController(_paths, picker, runner, reports);
        _postfixLab = new PostfixLabController(_paths, picker, expressions, reports);
        _infixLab = new InfixLabController(_paths, picker, expressions, reports);
    }

    public void Run()
    {
        while (true)
        {
            ConsoleTheme.BeginScreen("Главное меню");
            ConsoleTheme.Title("Что будем исследовать?");
            ConsoleTheme.MenuOption("1", "Стек", "операции 1–5, замеры, CSV");
            ConsoleTheme.MenuOption("2", "Очередь", "связный список и стандартный Queue<T>");
            ConsoleTheme.MenuOption("3", "Постфиксные выражения", "вычисление и оценка сложности");
            ConsoleTheme.MenuOption("4", "Инфикс → постфикс", "ввод формулы и сохранение в InfixInput");
            ConsoleTheme.MenuOption("0", "Выход");
            ConsoleTheme.Rule();

            var choice = ConsoleInput.ReadMenuChoice(4);

            switch (choice)
            {
                case 0:
                    ShowFarewell();
                    return;
                case 1:
                    _stackLab.Run();
                    break;
                case 2:
                    _queueLab.Run();
                    break;
                case 3:
                    _postfixLab.Run();
                    break;
                case 4:
                    _infixLab.Run();
                    break;
            }
        }
    }

    private void ShowFarewell()
    {
        ConsoleTheme.BeginScreen("Главное меню");
        ConsoleTheme.Title("Работа завершена");
        ConsoleTheme.Muted($"  Отчёты сохранены в: {_paths.Output}");
        Console.WriteLine();
        ConsoleTheme.Success("До свидания.");
    }
}
