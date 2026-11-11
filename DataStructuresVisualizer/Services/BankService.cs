using System.Text;
using DataStructuresVisualizer.Models;

namespace DataStructuresVisualizer.Services;

public static class BankService
{
    public static IReadOnlyList<BankCustomer> GenerateRandom(Random? random = null, int workdayMinutes = 480)
    {
        random ??= Random.Shared;

        var names = new[]
        {
            "Анна", "Борис", "Вера", "Глеб", "Дарья", "Егор", "Жанна", "Зоя",
            "Ирина", "Кирилл", "Лев", "Мария", "Никита", "Ольга", "Пётр"
        };

        var result = new List<BankCustomer>();
        var minute = random.Next(0, 8);

        while (minute < workdayMinutes)
        {
            result.Add(new BankCustomer
            {
                Name = names[random.Next(names.Length)],
                ArrivalMinute = minute,
                ServiceMinutes = random.Next(2, 9)
            });

            minute += random.Next(3, 16);
        }

        return result;
    }

    public static bool TryParse(string text, out IReadOnlyList<BankCustomer> customers, out string error)
    {
        customers = Array.Empty<BankCustomer>();
        error = string.Empty;

        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<BankCustomer>();

        for (var i = 0; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',', StringSplitOptions.TrimEntries);

            if (parts.Length != 3 ||
                !int.TryParse(parts[1], out var arrival) ||
                !int.TryParse(parts[2], out var service) ||
                arrival < 0 ||
                service <= 0)
            {
                error = $"Строка {i + 1}: нужен формат «Имя, время прихода, длительность».";
                return false;
            }

            if (string.IsNullOrWhiteSpace(parts[0]))
            {
                error = $"Строка {i + 1}: имя клиента пустое.";
                return false;
            }

            result.Add(new BankCustomer
            {
                Name = parts[0],
                ArrivalMinute = arrival,
                ServiceMinutes = service
            });
        }

        customers = result.OrderBy(c => c.ArrivalMinute).ToArray();
        return true;
    }

    public static string ToInput(IReadOnlyList<BankCustomer> customers)
    {
        var builder = new StringBuilder();

        foreach (var customer in customers)
        {
            builder.Append(customer.Name)
                .Append(", ")
                .Append(customer.ArrivalMinute)
                .Append(", ")
                .Append(customer.ServiceMinutes)
                .Append(Environment.NewLine);
        }

        return builder.ToString();
    }
}
