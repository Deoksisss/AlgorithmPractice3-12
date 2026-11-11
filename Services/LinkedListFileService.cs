using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StackQueueLab.Services
{
    public static class LinkedListFileService
    {
        public static LinkedList<int> ReadIntList(string path)
        {
            var list = new LinkedList<int>();
            if (!File.Exists(path)) return list;

            string text = File.ReadAllText(path);
            string[] tokens = text.Split(new[] { ' ', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (var t in tokens)
            {
                // Используем стандартный метод AddLast вместо PushBack
                if (int.TryParse(t, out var v)) 
                    list.AddLast(v);
            }
            return list;
        }

        public static List<LinkedList<int>> ReadSets(string path)
        {
            if (!File.Exists(path)) return new List<LinkedList<int>>();

            return File.ReadAllLines(path)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(line =>
                {
                    var l = new LinkedList<int>();
                    string[] tokens = line.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var t in tokens)
                    {
                        // Используем стандартный метод AddLast вместо PushBack
                        if (int.TryParse(t, out var v)) 
                            l.AddLast(v);
                    }
                    return l;
                })
                .ToList();
        }
    }
}