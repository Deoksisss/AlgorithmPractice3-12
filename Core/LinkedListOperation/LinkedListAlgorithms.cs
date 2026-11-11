using System;

namespace StackQueueLab.Core.LinkedListOperations
{
    public static class LinkedListAlgorithms
    {
        // 9. Дописать E к L. O(1)
        public static void AppendList<T>(StackQueueLab.Core.Structures.LinkedList<T> l, StackQueueLab.Core.Structures.LinkedList<T> e)
        {
            if (e.IsEmpty) return;

            if (l.IsEmpty)
            {
                l.SetHeadTail(e.Head, e.Tail, e.Count);
            }
            else
            {
                if (l.Tail != null)
                {
                    l.Tail.Next = e.Head;
                }
                l.SetHeadTail(l.Head, e.Tail, l.Count + e.Count);
            }
            e.Clear();
        }

        // 10. Разбиение по первому вхождению x. O(n)
        public static void SplitByFirstOccurrence<T>(StackQueueLab.Core.Structures.LinkedList<T> source, T x, StackQueueLab.Core.Structures.LinkedList<T> a, StackQueueLab.Core.Structures.LinkedList<T> b)
            where T : IEquatable<T>
        {
            a.Clear(); 
            b.Clear();

            var cur = source.Head;
            StackQueueLab.Core.Structures.LinkedList<T>.Node? prev = null;
            
            while (cur != null && !cur.Value.Equals(x)) 
            { 
                prev = cur; 
                cur = cur.Next; 
            }

            if (cur == null)
            {
                a.SetHeadTail(source.Head, source.Tail, source.Count);
            }
            else
            {
                int aCount = 0;
                var p = source.Head;
                while (p != null && p != cur) 
                { 
                    aCount++; 
                    p = p.Next; 
                }
                
                a.SetHeadTail(source.Head, prev, aCount);
                if (a.Tail != null) a.Tail.Next = null;

                b.SetHeadTail(cur, source.Tail, source.Count - aCount);
            }
            source.Clear();
        }

        // 11. Удвоить список. O(n)
        public static void DoubleList<T>(StackQueueLab.Core.Structures.LinkedList<T> list)
        {
            if (list.IsEmpty) return;
            var cur = list.Head;
            int n = list.Count;
            for (int i = 0; i < n && cur != null; i++)
            {
                list.PushBack(cur.Value);
                cur = cur.Next;
            }
        }

        // 12. Обмен значений. O(n)
        public static bool SwapValues<T>(StackQueueLab.Core.Structures.LinkedList<T> list, T x, T y)
            where T : IEquatable<T>
        {
            if (x.Equals(y)) return false;
            
            StackQueueLab.Core.Structures.LinkedList<T>.Node? a = null;
            StackQueueLab.Core.Structures.LinkedList<T>.Node? b = null;     
            
            for (StackQueueLab.Core.Structures.LinkedList<T>.Node? cur = list.Head; cur != null; cur = cur.Next)
            {
                if (cur.Value != null && cur.Value.Equals(x)) a = cur;
                if (cur.Value != null && cur.Value.Equals(y)) b = cur;
            }
            
            if (a == null || b == null) return false;
            
            T temp = a.Value;
            a.Value = b.Value;
            b.Value = temp;
            
            return true;
        }
    }
}