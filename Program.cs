using StackQueueLab.Diagnostics;
using StackQueueLab.Services;
using StackQueueLab.UI;
using StackQueueLab.UI.Controllers;

namespace StackQueueLab;

public static class Program
{
    public static int Main(string[] args)
    {
        ConsoleTheme.Initialize();

        try
        {
            if (args.Any(arg => arg.Equals("--linked-list-test", StringComparison.OrdinalIgnoreCase)))
            {
                return LinkedListSelfTestRunner.Run();
            }

            if (args.Any(arg => arg.Equals("--self-test", StringComparison.OrdinalIgnoreCase)))
            {
                var includeExtreme = args.Any(arg =>
                    arg.Equals("--include-extreme", StringComparison.OrdinalIgnoreCase));

                return SelfTestRunner.Run(ProjectPaths.Create(), includeExtreme);
            }

            new Application().Run();
            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine();
            ConsoleTheme.Error("Непредвиденная ошибка:");
            ConsoleTheme.Muted(exception.ToString());
            ConsoleTheme.Pause();
            return 1;
        }
    }
}
