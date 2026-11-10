namespace StackQueueLab.Models;

/// <summary>
/// Одна операция входного файла: код 1–5 и значение для добавления.
/// </summary>
public sealed record CollectionOperation(int Code, string? Value)
{
    public string Describe() => Code == 1 ? $"1,{Value}" : Code.ToString();
}
