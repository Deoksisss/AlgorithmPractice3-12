using StackQueueLab.Core.Structures;
using StackQueueLab.Models;
using StackQueueLab.Services;

namespace StackQueueLab.Diagnostics;

/// <summary>
/// Скрытая диагностика: dotnet run -- --self-test.
/// Проверяет структуры, алгоритмы и все тестовые файлы.
/// </summary>
public static class SelfTestRunner
{
    public static int Run(ProjectPaths paths, bool includeExtreme)
    {
        var failures = 0;
        Console.WriteLine("SELF TEST");
        Console.WriteLine("=========");

        failures += CheckCoreStructures();
        failures += CheckExpressions();
        failures += CheckOperationFolder(paths.StackInput, "стек", includeExtreme);
        failures += CheckOperationFolder(paths.QueueInput, "очередь", includeExtreme);
        failures += CheckPostfixFolder(paths.PostfixInput, includeExtreme);
        failures += CheckInfixFolder(paths.InfixInput, includeExtreme);

        Console.WriteLine();
        Console.WriteLine(failures == 0
            ? "РЕЗУЛЬТАТ: все проверки пройдены."
            : $"РЕЗУЛЬТАТ: ошибок — {failures}.");

        return failures == 0 ? 0 : 1;
    }

    private static int CheckCoreStructures()
    {
        var failures = 0;

        try
        {
            var stack = new LinkedStack<int>();
            stack.Push(1);
            stack.Push(2);

            if (stack.Count != 2 || stack.Peek() != 2 || stack.Pop() != 2 || stack.Pop() != 1 || !stack.IsEmpty)
            {
                throw new InvalidOperationException("Неверный порядок стека.");
            }

            var queue = new LinkedQueue<int>();
            queue.Enqueue(1);
            queue.Enqueue(2);

            if (queue.Count != 2 || queue.Peek() != 1 || queue.Dequeue() != 1 || queue.Dequeue() != 2 || !queue.IsEmpty)
            {
                throw new InvalidOperationException("Неверный порядок очереди.");
            }

            Console.WriteLine("OK  структуры: стек LIFO, очередь FIFO");
        }
        catch (Exception exception)
        {
            failures++;
            Console.WriteLine($"ERR структуры: {exception.Message}");
        }

        return failures;
    }

    private static int CheckExpressions()
    {
        var failures = 0;
        var converter = new InfixToPostfixConverter();
        var evaluator = new PostfixEvaluator();

        var cases = new (string Infix, string Postfix, double Value)[]
        {
            ("2+2", "2 2 +", 4),
            ("2+2*2", "2 2 2 * +", 6),
            ("(2+2)*2", "2 2 + 2 *", 8),
            ("2^3^2", "2 3 2 ^ ^", 512),
            ("2^-3", "2 3 u- ^", 0.125),
            ("-2^2", "2 2 ^ u-", -4),
            ("sin(0.5)", "0.5 sin", Math.Sin(0.5)),
            ("sqrt(9)+1", "9 sqrt 1 +", 4)
        };

        foreach (var item in cases)
        {
            try
            {
                var postfix = converter.Convert(item.Infix);
                var value = evaluator.Evaluate(postfix);

                if (postfix != item.Postfix || Math.Abs(value - item.Value) > 1e-9)
                {
                    failures++;
                    Console.WriteLine(
                        $"ERR выражение «{item.Infix}»: получено «{postfix}» = {value}, ожидалось «{item.Postfix}» = {item.Value}");
                }
            }
            catch (Exception exception)
            {
                failures++;
                Console.WriteLine($"ERR выражение «{item.Infix}»: {exception.Message}");
            }
        }

        if (failures == 0)
        {
            Console.WriteLine($"OK  выражения: {cases.Length} контрольных примеров");
        }

        return failures;
    }

    private static bool IsExtreme(string fileName) =>
        fileName.Contains("EXTREME", StringComparison.OrdinalIgnoreCase);

    private static int CheckOperationFolder(string folder, string label, bool includeExtreme)
    {
        var parser = new OperationFileParser();
        var files = FileCatalog.ListTextFiles(folder);
        var failures = 0;

        foreach (var file in files)
        {
            if (IsExtreme(file.Name) && !includeExtreme)
            {
                Console.WriteLine($"SKIP {label,-7} {file.Name,-42} стресс-файл");
                continue;
            }

            var parsed = parser.Parse(file.Path);
            var expectedInvalid = file.Name.Contains("invalid", StringComparison.OrdinalIgnoreCase);
            var passed = parsed.IsValid != expectedInvalid;
            var note = expectedInvalid ? "  [ожидаемая ошибка]" : string.Empty;

            Console.WriteLine(
                $"{(passed ? "OK " : "ERR")} {label,-7} {file.Name,-42} " +
                $"наборов={parsed.Sets.Count}, операций={parsed.TotalOperations}, " +
                $"замечаний={parsed.Issues.Count}{note}");

            if (!passed)
            {
                failures++;
            }
        }

        return failures;
    }

    private static int CheckPostfixFolder(string folder, bool includeExtreme)
    {
        var validator = new PostfixValidator();
        var evaluator = new PostfixEvaluator();
        var failures = 0;

        foreach (var file in FileCatalog.ListTextFiles(folder))
        {
            if (IsExtreme(file.Name) && !includeExtreme)
            {
                Console.WriteLine($"SKIP постфикс {file.Name,-42} стресс-файл");
                continue;
            }

            var lines = File.ReadAllLines(file.Path);
            var errors = 0;
            var evaluated = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                var validation = validator.Validate(lines[i]);

                if (!validation.IsValid)
                {
                    errors++;
                    if (errors <= 3)
                    {
                        Console.WriteLine($"ERR {file.Name}:{i + 1} {validation.Error}");
                    }

                    continue;
                }

                try
                {
                    evaluator.Evaluate(lines[i]);
                    evaluated++;
                }
                catch (ExpressionEvaluationException exception)
                {
                    errors++;
                    if (errors <= 3)
                    {
                        Console.WriteLine($"ERR {file.Name}:{i + 1} {exception.Message}");
                    }
                }
            }

            Console.WriteLine(
                $"{(errors == 0 ? "OK " : "ERR")} постфикс {file.Name,-42} " +
                $"вычислено={evaluated}, ошибок={errors}");

            if (errors > 0)
            {
                failures++;
            }
        }

        return failures;
    }

    private static int CheckInfixFolder(string folder, bool includeExtreme)
    {
        var service = new ExpressionFileService();
        var converter = new InfixToPostfixConverter();
        var evaluator = new PostfixEvaluator();
        var failures = 0;

        foreach (var file in FileCatalog.ListTextFiles(folder))
        {
            if (IsExtreme(file.Name) && !includeExtreme)
            {
                Console.WriteLine($"SKIP инфикс  {file.Name,-42} стресс-файл");
                continue;
            }

            var loaded = service.LoadInfix(file.Path);

            if (!loaded.IsValid)
            {
                Console.WriteLine($"ERR инфикс {file.Name}: {loaded.Issues.Count} ошибок разбора");
                failures++;
                continue;
            }

            var errors = 0;
            var evaluated = 0;

            foreach (var line in loaded.Lines)
            {
                try
                {
                    var postfix = converter.Convert(line.Expression);
                    evaluator.Evaluate(postfix);
                    evaluated++;
                }
                catch (Exception exception)
                {
                    errors++;
                    if (errors <= 3)
                    {
                        Console.WriteLine($"ERR {file.Name}:{line.SourceLine} {exception.Message}");
                    }
                }
            }

            Console.WriteLine(
                $"{(errors == 0 ? "OK " : "ERR")} инфикс  {file.Name,-42} " +
                $"вычислено={evaluated}, ошибок={errors}");

            if (errors > 0)
            {
                failures++;
            }
        }

        return failures;
    }
}
