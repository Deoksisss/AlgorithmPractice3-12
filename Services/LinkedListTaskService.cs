using StackQueueLab.Core.Structures;

namespace StackQueueLab.Services;

public sealed record LinkedListTaskResult(
    IReadOnlyList<int> Before,
    IReadOnlyList<int> After,
    IReadOnlySet<int> ChangedIndexes,
    string? Note);

public static class LinkedListTaskService
{
    private const int MinimumRandomLength = 7;
    private const int MaximumRandomLength = 25;
    private const int MinimumValue = 0;
    private const int MaximumValue = 999;

    public static IReadOnlyList<int> GenerateRandom(bool sorted = false)
    {
        var length = Random.Shared.Next(MinimumRandomLength, MaximumRandomLength + 1);
        var values = new int[length];

        for (var i = 0; i < length; i++)
        {
            values[i] = Random.Shared.Next(MinimumValue, MaximumValue + 1);
        }

        if (sorted)
        {
            Array.Sort(values);
        }

        return values;
    }

    public static bool TryParseManual(string input, out IReadOnlyList<int> values)
    {
        var parts = input.Split(
            [' ', '\t', '\n', '\r', ','],
            StringSplitOptions.RemoveEmptyEntries);

        var parsed = new List<int>(parts.Length);

        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var number))
            {
                values = Array.Empty<int>();
                return false;
            }

            parsed.Add(number);
        }

        values = parsed;
        return true;
    }

    public static bool IsSortedNondecreasing(IReadOnlyList<int> values)
    {
        for (var i = 1; i < values.Count; i++)
        {
            if (values[i] < values[i - 1])
            {
                return false;
            }
        }

        return true;
    }

    public static IReadOnlyList<int> SortNondecreasing(IReadOnlyList<int> values)
    {
        var copy = values.ToArray();
        Array.Sort(copy);
        return copy;
    }

    public static LinkedListTaskResult Execute(
        int taskNumber,
        IReadOnlyList<int> input,
        int variant = 0,
        int firstParameter = 0,
        int secondParameter = 0)
    {
        var before = input;
        var list = CreateList(before);
        var changed = new HashSet<int>();

        switch (taskNumber)
        {
            case 1:
                list.Reverse();
                var reversed = list.ToArray();

                for (var i = 0; i < reversed.Count; i++)
                {
                    if (i != reversed.Count - 1 - i)
                    {
                        changed.Add(i);
                    }
                }

                return new LinkedListTaskResult(
                    before,
                    reversed,
                    changed,
                    before.Count == 0 ? "Список пуст." : null);

            case 2:
                if (variant == 1)
                {
                    list.MoveLastToFront();
                }
                else
                {
                    list.MoveFirstToLast();
                }

                var moved = list.ToArray();

                if (moved.Count > 1)
                {
                    changed.Add(variant == 1 ? 0 : moved.Count - 1);
                }

                return new LinkedListTaskResult(
                    before,
                    moved,
                    changed,
                    before.Count <= 1 ? "Перенос не требуется: элемент не меняет своё положение." : null);

            case 3:
                return new LinkedListTaskResult(
                    before,
                    before,
                    changed,
                    $"Количество различных элементов: {list.CountDistinct()}.");

            case 4:
                list.RemoveNonUnique();
                var uniqueOnly = list.ToArray();
                var removedNonUnique = before.Count - uniqueOnly.Count;

                return new LinkedListTaskResult(
                    before,
                    uniqueOnly,
                    changed,
                    removedNonUnique == 0
                        ? "Неуникальных элементов не найдено."
                        : $"Удалено элементов: {removedNonUnique}. Сдвинувшиеся элементы не выделяются.");

            case 5:
                list.InsertSelfAfter(firstParameter);
                var insertedAfterX = list.ToArray();
                var xIndex = FindIndex(before, firstParameter);

                if (xIndex >= 0)
                {
                    for (var i = xIndex + 1; i <= xIndex + before.Count; i++)
                    {
                        changed.Add(i);
                    }
                }

                return new LinkedListTaskResult(
                    before,
                    insertedAfterX,
                    changed,
                    xIndex < 0 ? $"Значение {firstParameter} не найдено." : null);

            case 6:
                list.InsertSorted(firstParameter);
                var sortedInsert = list.ToArray();
                var insertionIndex = before.Count;

                for (var i = 0; i < before.Count; i++)
                {
                    if (before[i] >= firstParameter)
                    {
                        insertionIndex = i;
                        break;
                    }
                }

                if (sortedInsert.Count == before.Count + 1)
                {
                    changed.Add(insertionIndex);
                }

                return new LinkedListTaskResult(
                    before,
                    sortedInsert,
                    changed,
                    IsSortedNondecreasing(before)
                        ? null
                        : "Внимание: алгоритм рассчитан на неубывающий список.");

            case 7:
                list.RemoveAll(firstParameter);
                var afterRemove = list.ToArray();
                var removedCount = before.Count - afterRemove.Count;

                return new LinkedListTaskResult(
                    before,
                    afterRemove,
                    changed,
                    removedCount == 0
                        ? $"Значение {firstParameter} не найдено."
                        : $"Удалено элементов: {removedCount}. Сдвинувшиеся элементы не выделяются.");

            case 8:
                list.InsertBefore(firstParameter, secondParameter);
                var insertedBefore = list.ToArray();
                var eIndex = FindIndex(before, secondParameter);

                if (eIndex >= 0 && insertedBefore.Count == before.Count + 1)
                {
                    changed.Add(eIndex);
                }

                return new LinkedListTaskResult(
                    before,
                    insertedBefore,
                    changed,
                    eIndex < 0 ? $"Значение {secondParameter} не найдено." : null);

            default:
                return new LinkedListTaskResult(before, before, changed, "Неизвестное задание.");
        }
    }

    private static CustomLinkedList CreateList(IReadOnlyList<int> values)
    {
        var list = new CustomLinkedList();

        foreach (var value in values)
        {
            list.Add(value);
        }

        return list;
    }

    private static int FindIndex(IReadOnlyList<int> values, int target)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] == target)
            {
                return i;
            }
        }

        return -1;
    }
}
