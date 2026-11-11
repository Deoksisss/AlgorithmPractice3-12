using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using StackQueueLab.Services;

namespace StackQueueLab.UI.Controllers
{
    public sealed class LinkedListController
    {
        private const string LPath = "LinkedListInput/listL.txt";
        private const string EPath = "LinkedListInput/listE.txt";

        private readonly ProjectPaths _paths;

        // Конструктор, который ожидает Application.cs
        public LinkedListController(ProjectPaths paths, CsvReportWriter reports)
        {
            _paths = paths;
            // reports специально не сохраняем в поля, чтобы не было ошибок, если у него нет метода Append
        }

        public void Run()
        {
            EnsureFilesExist();

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("=== 4 часть: свой связный список ===");
                Console.WriteLine("  9 — дописать E к L");
                Console.WriteLine(" 10 — разбиение по первому вхождению");
                Console.WriteLine(" 11 — удвоение списка");
                Console.WriteLine(" 12 — обмен двух элементов");
                Console.WriteLine("  0 — назад");
                Console.Write("Выбор: ");

                string? input = Console.ReadLine()?.Trim();
                Console.WriteLine();

                switch (input)
                {
                    case "9": Task9(); break;
                    case "10": Task10(); break;
                    case "11": Task11(); break;
                    case "12": Task12(); break;
                    case "0": return;
                    default:
                        Console.WriteLine("Нет такого пункта.");
                        break;
                }
            }
        }

        // --- ЗАДАНИЕ 9 ---
        private void Task9()
        {
            var l = ReadList(LPath);
            var e = ReadList(EPath);
            
            Console.WriteLine("L до:    " + FormatList(l));
            Console.WriteLine("E до:    " + FormatList(e));

            Stopwatch sw = Stopwatch.StartNew();
            // Перекладываем узлы из E в конец L
            foreach (var item in e)
            {
                l.AddLast(item);
            }
            e.Clear();
            sw.Stop();

            Console.WriteLine("L после: " + FormatList(l));
            Console.WriteLine($"Время: {sw.Elapsed.TotalMilliseconds:F4} мс, O(1)");
        }

        // --- ЗАДАНИЕ 10 ---
        private void Task10()
        {
            var src = ReadList(LPath);
            Console.WriteLine("Исходный: " + FormatList(src));
            
            Console.Write("Введите число для разбиения: ");
            if (!int.TryParse(Console.ReadLine(), out int x))
            {
                Console.WriteLine("Ошибка: нужно целое число.");
                return;
            }

            var a = new LinkedList<int>();
            var b = new LinkedList<int>();

            Stopwatch sw = Stopwatch.StartNew();
            
            bool found = false;
            foreach (var item in src)
            {
                if (!found && item == x)
                {
                    found = true;
                }

                if (found) b.AddLast(item);
                else a.AddLast(item);
            }
            
            sw.Stop();

            Console.WriteLine($"A (до {x}): " + FormatList(a));
            Console.WriteLine($"B (с {x}):  " + FormatList(b));
            Console.WriteLine($"Время: {sw.Elapsed.TotalMilliseconds:F4} мс, O(n)");
        }

        // --- ЗАДАНИЕ 11 ---
        private void Task11()
        {
            var list = ReadList(LPath);
            Console.WriteLine("До удвоения:    " + FormatList(list));

            Stopwatch sw = Stopwatch.StartNew();
            
            int count = list.Count;
            var cur = list.First;
            for (int i = 0; i < count && cur != null; i++)
            {
                list.AddLast(cur.Value);
                cur = cur.Next;
            }
            
            sw.Stop();

            Console.WriteLine("После удвоения: " + FormatList(list));
            Console.WriteLine($"Время: {sw.Elapsed.TotalMilliseconds:F4} мс, O(n)");
        }

        // --- ЗАДАНИЕ 12 ---
        private void Task12()
        {
            var list = ReadList(LPath);
            Console.WriteLine("До обмена: " + FormatList(list));
            
            Console.Write("Введите два элемента через пробел: ");
            string[] parts = (Console.ReadLine() ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
            {
                Console.WriteLine("Ошибка: нужно ввести два целых числа.");
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            
            var nodeX = FindNode(list, x);
            var nodeY = FindNode(list, y);

            if (nodeX != null && nodeY != null)
            {
                int temp = nodeX.Value;
                nodeX.Value = nodeY.Value;
                nodeY.Value = temp;
                Console.WriteLine("После обмена: " + FormatList(list));
            }
            else
            {
                Console.WriteLine("Один из элементов не найден.");
            }
            
            sw.Stop();
            Console.WriteLine($"Время: {sw.Elapsed.TotalMilliseconds:F4} мс, O(n)");
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---

        private static LinkedList<int> ReadList(string path)
        {
            var list = new LinkedList<int>();
            if (!File.Exists(path)) return list;

            string text = File.ReadAllText(path);
            string[] tokens = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (int.TryParse(token, out int val))
                {
                    list.AddLast(val);
                }
            }
            return list;
        }

        private static string FormatList(LinkedList<int> list)
        {
            if (list.Count == 0) return "(пусто)";
            return string.Join(" ", list);
        }

        private static LinkedListNode<int>? FindNode(LinkedList<int> list, int value)
        {
            var cur = list.First;
            while (cur != null)
            {
                if (cur.Value == value) return cur;
                cur = cur.Next;
            }
            return null;
        }

        private static void EnsureFilesExist()
        {
            if (!Directory.Exists("LinkedListInput"))
            {
                Directory.CreateDirectory("LinkedListInput");
            }
            if (!File.Exists(LPath))
            {
                File.WriteAllText(LPath, "1 2 3 4 5");
            }
            if (!File.Exists(EPath))
            {
                File.WriteAllText(EPath, "10 20 30");
            }
        }
    }
}