using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace StackQueueLab.Core.Structures
{
    public class LinkedList<T> : IEnumerable<T>
    {
        // Делаем Node internal, чтобы алгоритмы могли работать со ссылками
        internal class Node
        {
            public T Value;
            public Node? Next;
            public Node(T value) => Value = value;
        }

        private Node? _head;
        private Node? _tail;

        public int Count { get; private set; }
        public bool IsEmpty => _head == null;

        public void PushBack(T value)
        {
            var node = new Node(value);
            if (_head == null) 
                _head = _tail = node;
            else 
            { 
                _tail!.Next = node; 
                _tail = node; 
            }
            Count++;
        }

        public void Clear()
        {
            _head = _tail = null;
            Count = 0;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var cur = _head; cur != null; cur = cur.Next)
                sb.Append(cur.Value).Append(' ');
            return sb.ToString().TrimEnd();
        }

        // Внутренний доступ для алгоритмов п.9–12
        internal Node? Head => _head;
        internal Node? Tail => _tail;

        internal void SetHeadTail(Node? head, Node? tail, int count)
        {
            _head = head; 
            _tail = tail; 
            Count = count;
        }

        // Реализация IEnumerable, чтобы можно было использовать foreach
        public IEnumerator<T> GetEnumerator()
        {
            var cur = _head;
            while (cur != null)
            {
                yield return cur.Value;
                cur = cur.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}