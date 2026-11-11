using Avalonia.Controls;

namespace DataStructuresVisualizer.Models;

public sealed class BankCustomer
{
    public required string Name { get; init; }

    public required int ArrivalMinute { get; init; }

    public required int ServiceMinutes { get; init; }

    public int RemainingServiceMinutes { get; set; }

    public int? WaitedMinutes { get; set; }

    public bool IsServed { get; set; }

    public Border? Visual { get; set; }
}
