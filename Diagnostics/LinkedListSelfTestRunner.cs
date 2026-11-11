using StackQueueLab.Core.Structures;

namespace StackQueueLab.Diagnostics;

/// <summary>
/// Автономная проверка заданий 1–8 для CustomLinkedList.
/// Запуск: dotnet run -- --linked-list-test
/// </summary>
public static class LinkedListSelfTestRunner
{
    public static int Run()
    {
        Console.WriteLine("CustomLinkedList SELF TEST");
        Console.WriteLine("==========================");
        Console.WriteLine();

        var failures = 0;

        failures += CheckReverse();
        failures += CheckMove();
        failures += CheckCountDistinct();
        failures += CheckRemoveNonUnique();
        failures += CheckInsertSelfAfter();
        failures += CheckInsertSorted();
        failures += CheckRemoveAll();
        failures += CheckInsertBefore();

        Console.WriteLine();
        Console.WriteLine(failures == 0
            ? "РЕЗУЛЬТАТ: все проверки списка пройдены."
            : $"РЕЗУЛЬТАТ: ошибок — {failures}.");

        return failures == 0 ? 0 : 1;
    }

    private static int CheckReverse()
    {
        Console.WriteLine("Задание 1. Переворот списка");

        var failures = 0;

        var list = From(1, 2, 3, 4);
        list.Reverse();
        failures += CheckList("  обычный список", list, new[] { 4, 3, 2, 1 });

        list.Reverse();
        failures += CheckList("  повторный переворот", list, new[] { 1, 2, 3, 4 });

        list = From(5);
        list.Reverse();
        failures += CheckList("  один элемент", list, new[] { 5 });

        list = From();
        list.Reverse();
        failures += CheckList("  пустой список", list, Array.Empty<int>());

        Console.WriteLine();
        return failures;
    }

    private static int CheckMove()
    {
        Console.WriteLine("Задание 2. Перенос крайнего элемента");

        var failures = 0;

        var list = From(1, 2, 3, 4);
        list.MoveLastToFront();
        failures += CheckList("  последний в начало", list, new[] { 4, 1, 2, 3 });

        list = From(1);
        list.MoveLastToFront();
        failures += CheckList("  один элемент без изменений", list, new[] { 1 });

        list = From();
        list.MoveLastToFront();
        failures += CheckList("  пустой список без изменений", list, Array.Empty<int>());

        list = From(1, 2, 3, 4);
        list.MoveFirstToLast();
        failures += CheckList("  первый в конец", list, new[] { 2, 3, 4, 1 });

        list = From(1);
        list.MoveFirstToLast();
        failures += CheckList("  один элемент без изменений", list, new[] { 1 });

        list = From();
        list.MoveFirstToLast();
        failures += CheckList("  пустой список без изменений", list, Array.Empty<int>());

        Console.WriteLine();
        return failures;
    }

    private static int CheckCountDistinct()
    {
        Console.WriteLine("Задание 3. Количество различных элементов");

        var failures = 0;

        failures += CheckValue("  пустой список", From().CountDistinct(), 0);
        failures += CheckValue("  все уникальны", From(1, 2, 3, 4).CountDistinct(), 4);
        failures += CheckValue("  есть повторы", From(1, 2, 2, 3, 3, 3).CountDistinct(), 3);
        failures += CheckValue("  один элемент", From(42).CountDistinct(), 1);

        Console.WriteLine();
        return failures;
    }

    private static int CheckRemoveNonUnique()
    {
        Console.WriteLine("Задание 4. Удаление неуникальных элементов");

        var failures = 0;

        var list = From(1, 2, 2, 3);
        list.RemoveNonUnique();
        failures += CheckList("  обычный список", list, new[] { 1, 3 });

        list = From(1, 2, 3);
        list.RemoveNonUnique();
        failures += CheckList("  все уникальны", list, new[] { 1, 2, 3 });

        list = From(2, 2, 2);
        list.RemoveNonUnique();
        failures += CheckList("  все одинаковы", list, Array.Empty<int>());

        list = From(2, 2, 1, 2);
        list.RemoveNonUnique();
        failures += CheckList("  уникальный в середине", list, new[] { 1 });

        list = From();
        list.RemoveNonUnique();
        failures += CheckList("  пустой список", list, Array.Empty<int>());

        Console.WriteLine();
        return failures;
    }

    private static int CheckInsertSelfAfter()
    {
        Console.WriteLine("Задание 5. Вставка списка после первого x");

        var failures = 0;

        var list = From(1, 2, 3, 4);
        list.InsertSelfAfter(3);
        failures += CheckList("  x в середине", list, new[] { 1, 2, 3, 1, 2, 3, 4, 4 });

        list = From(1, 2, 3, 4);
        list.InsertSelfAfter(1);
        failures += CheckList("  x в начале", list, new[] { 1, 1, 2, 3, 4, 2, 3, 4 });

        list = From(1, 2, 3, 4);
        list.InsertSelfAfter(4);
        failures += CheckList("  x в конце", list, new[] { 1, 2, 3, 4, 1, 2, 3, 4 });

        list = From(1, 2, 2, 3);
        list.InsertSelfAfter(2);
        failures += CheckList("  только первое вхождение", list, new[] { 1, 2, 1, 2, 2, 3, 2, 3 });

        list = From(1, 2, 3);
        list.InsertSelfAfter(99);
        failures += CheckList("  x отсутствует", list, new[] { 1, 2, 3 });

        list = From();
        list.InsertSelfAfter(1);
        failures += CheckList("  пустой список", list, Array.Empty<int>());

        Console.WriteLine();
        return failures;
    }

    private static int CheckInsertSorted()
    {
        Console.WriteLine("Задание 6. Вставка в упорядоченный список");

        var failures = 0;

        var list = From(1, 3, 5, 7);
        list.InsertSorted(6);
        failures += CheckList("  в середину", list, new[] { 1, 3, 5, 6, 7 });

        list = From(1, 3, 5, 7);
        list.InsertSorted(0);
        failures += CheckList("  в начало", list, new[] { 0, 1, 3, 5, 7 });

        list = From(1, 3, 5, 7);
        list.InsertSorted(9);
        failures += CheckList("  в конец", list, new[] { 1, 3, 5, 7, 9 });

        list = From(1, 3, 5, 7);
        list.InsertSorted(3);
        failures += CheckList("  рядом с дубликатом", list, new[] { 1, 3, 3, 5, 7 });

        list = From();
        list.InsertSorted(5);
        failures += CheckList("  пустой список", list, new[] { 5 });

        Console.WriteLine();
        return failures;
    }

    private static int CheckRemoveAll()
    {
        Console.WriteLine("Задание 7. Удаление всех элементов e");

        var failures = 0;

        var list = From(1, 2, 2, 3, 2, 4);
        list.RemoveAll(2);
        failures += CheckList("  обычное удаление", list, new[] { 1, 3, 4 });

        list = From(1, 2, 2, 3);
        list.RemoveAll(1);
        failures += CheckList("  удаление из начала", list, new[] { 2, 2, 3 });

        list = From(1, 2, 2, 3);
        list.RemoveAll(3);
        failures += CheckList("  удаление из конца", list, new[] { 1, 2, 2 });

        list = From(2, 2, 2);
        list.RemoveAll(2);
        failures += CheckList("  все элементы равны e", list, Array.Empty<int>());

        list = From(1, 2, 3);
        list.RemoveAll(99);
        failures += CheckList("  e отсутствует", list, new[] { 1, 2, 3 });

        list = From();
        list.RemoveAll(1);
        failures += CheckList("  пустой список", list, Array.Empty<int>());

        Console.WriteLine();
        return failures;
    }

    private static int CheckInsertBefore()
    {
        Console.WriteLine("Задание 8. Вставка f перед первым e");

        var failures = 0;

        var list = From(1, 2, 3, 4);
        list.InsertBefore(9, 3);
        failures += CheckList("  перед серединой", list, new[] { 1, 2, 9, 3, 4 });

        list = From(1, 2, 3, 4);
        list.InsertBefore(9, 1);
        failures += CheckList("  перед первым элементом", list, new[] { 9, 1, 2, 3, 4 });

        list = From(1, 2, 3, 4);
        list.InsertBefore(9, 4);
        failures += CheckList("  перед последним элементом", list, new[] { 1, 2, 3, 9, 4 });

        list = From(1, 2, 2, 3);
        list.InsertBefore(5, 2);
        failures += CheckList("  только перед первым вхождением", list, new[] { 1, 5, 2, 2, 3 });

        list = From(1, 2, 3);
        list.InsertBefore(9, 99);
        failures += CheckList("  e отсутствует", list, new[] { 1, 2, 3 });

        list = From();
        list.InsertBefore(9, 1);
        failures += CheckList("  пустой список", list, Array.Empty<int>());

        Console.WriteLine();
        return failures;
    }

    private static CustomLinkedList From(params int[] values)
    {
        var list = new CustomLinkedList();

        foreach (var value in values)
        {
            list.Add(value);
        }

        return list;
    }

    private static int CheckList(string name, CustomLinkedList list, IReadOnlyList<int> expected)
    {
        var actual = list.ToArray();

        if (list.Count == expected.Count
            && actual.Count == expected.Count
            && actual.SequenceEqual(expected))
        {
            Console.WriteLine($"OK  {name}");
            return 0;
        }

        Console.WriteLine(
            $"ERR {name}: ожидалось [{Join(expected)}], " +
            $"получилось [{Join(actual)}], Count={list.Count}/{expected.Count}");

        return 1;
    }

    private static int CheckValue(string name, int actual, int expected)
    {
        if (actual == expected)
        {
            Console.WriteLine($"OK  {name}: {actual}");
            return 0;
        }

        Console.WriteLine($"ERR {name}: ожидалось {expected}, получилось {actual}");
        return 1;
    }

    private static string Join(IReadOnlyList<int> values) =>
        string.Join(", ", values);
}
