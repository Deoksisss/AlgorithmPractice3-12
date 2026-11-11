using System;
using System.Collections.Generic;

namespace StackQueueLab.Core.Structures;

/// <summary>
/// Собственный односвязный список с реализацией всех 12 заданий из 4-й части.
/// </summary>
public class CustomLinkedList
{
    private class Node
    {
        public int Value;
        public Node? Next;

        public Node(int value)
        {
            Value = value;
            Next = null;
        }
    }

    private Node? _head;
    public int Count { get; private set; }

    // --- Базовые методы ---

    public void Add(int value)
    {
        var newNode = new Node(value);
        if (_head == null)
        {
            _head = newNode;
        }
        else
        {
            var current = _head;
            while (current.Next != null) current = current.Next;
            current.Next = newNode;
        }
        Count++;
    }

    public void Print()
    {
        if (_head == null)
        {
            Console.WriteLine("Список пуст.");
            return;
        }

        var current = _head;
        Console.Write("Список: ");
        while (current != null)
        {
            Console.Write(current.Value + " ");
            current = current.Next;
        }
        Console.WriteLine();
    }

    // 1. Переворот списка
    public void Reverse()
    {
        Node? prev = null;
        Node? current = _head;
        while (current != null)
        {
            Node? next = current.Next;
            current.Next = prev;
            prev = current;
            current = next;
        }
        _head = prev;
    }

    // 2. Перенос последнего элемента в начало
    public void MoveLastToFront()
    {
        if (_head == null || _head.Next == null) return;

        Node? prev = null;
        Node? curr = _head;
        while (curr.Next != null)
        {
            prev = curr;
            curr = curr.Next;
        }

        prev!.Next = null;
        curr.Next = _head;
        _head = curr;
    }

    // 3. Подсчет количества различных элементов
    public int CountDistinct()
    {
        var seen = new HashSet<int>();
        var current = _head;
        while (current != null)
        {
            seen.Add(current.Value);
            current = current.Next;
        }
        return seen.Count;
    }

    // 4. Удаление неуникальных элементов
    public void RemoveNonUnique()
    {
        if (_head == null) return;

        var counts = new Dictionary<int, int>();
        var current = _head;
        while (current != null)
        {
            counts[current.Value] = counts.GetValueOrDefault(current.Value) + 1;
            current = current.Next;
        }

        while (_head != null && counts[_head.Value] > 1)
        {
            _head = _head.Next;
            Count--;
        }

        if (_head == null) return;

        current = _head;
        while (current.Next != null)
        {
            if (counts[current.Next.Value] > 1)
            {
                current.Next = current.Next.Next;
                Count--;
            }
            else
            {
                current = current.Next;
            }
        }
    }

    // 5. Вставка списка самого в себя после X
    public void InsertSelfAfter(int x)
    {
        if (_head == null) return;

        var current = _head;
        while (current != null && current.Value != x) current = current.Next;
        if (current == null) return;

        var cloneHead = CloneList(_head);
        if (cloneHead == null) return;

        var cloneTail = cloneHead;
        while (cloneTail.Next != null) cloneTail = cloneTail.Next;

        var nextAfterX = current.Next;
        current.Next = cloneHead;
        cloneTail.Next = nextAfterX;
        Count *= 2;
    }
    
    // 6. Вставка E в упорядоченный список
    public void InsertSorted(int e)
    {
        var newNode = new Node(e);
        if (_head == null || _head.Value >= e)
        {
            newNode.Next = _head;
            _head = newNode;
            Count++;
            return;
        }

        var current = _head;
        while (current.Next != null && current.Next.Value < e) current = current.Next;

        newNode.Next = current.Next;
        current.Next = newNode;
        Count++;
    }

    // 7. Удаление всех E
    public void RemoveAll(int e)
    {
        while (_head != null && _head.Value == e)
        {
            _head = _head.Next;
            Count--;
        }
        if (_head == null) return;

        var current = _head;
        while (current.Next != null)
        {
            if (current.Next.Value == e)
            {
                current.Next = current.Next.Next;
                Count--;
            }
            else current = current.Next;
        }
    }

    // 8. Вставка F перед первым вхождением E
    public void InsertBefore(int f, int e)
    {
        if (_head == null) return;

        if (_head.Value == e)
        {
            var newNode = new Node(f) { Next = _head };
            _head = newNode;
            Count++;
            return;
        }

        var current = _head;
        while (current.Next != null && current.Next.Value != e) current = current.Next;

        if (current.Next != null)
        {
            var newNode = new Node(f) { Next = current.Next };
            current.Next = newNode;
            Count++;
        }
    }

    // 9. Дописывание списка E в конец L
    public void AppendList(CustomLinkedList other)
    {
        if (other._head == null) return;

        var cloneOfOther = CloneList(other._head);
        if (cloneOfOther == null) return;

        if (_head == null) _head = cloneOfOther;
        else
        {
            var current = _head;
            while (current.Next != null) current = current.Next;
            current.Next = cloneOfOther;
        }
        Count += other.Count;
    }
    
    // 10. Разбиение списка по X
    public (CustomLinkedList left, CustomLinkedList right) SplitByValue(int x)
    {
        var left = new CustomLinkedList();
        var right = new CustomLinkedList();
        var current = _head;
        bool found = false;

        while (current != null)
        {
            if (!found)
            {
                left.Add(current.Value);
                if (current.Value == x) found = true;
            }
            else right.Add(current.Value);
            current = current.Next;
        }
        return (left, right);
    }

    // 11. Удвоение списка
    public void DoubleList()
    {
        if (_head == null) return;

        var clone = CloneList(_head);
        if (clone == null) return;

        var current = _head;
        while (current.Next != null) current = current.Next;
        current.Next = clone;
        Count *= 2;
    }
    
    // 12. Обмен местами двух элементов (по значениям)
    public void SwapValues(int val1, int val2)
    {
        Node? node1 = null;
        Node? node2 = null;
        var current = _head;

        while (current != null)
        {
            if (current.Value == val1) node1 = current;
            if (current.Value == val2) node2 = current;
            current = current.Next;
        }

        if (node1 != null && node2 != null)
        {
            int temp = node1.Value;
            node1.Value = node2.Value;
            node2.Value = temp;
        }
    }

    // Клонирование
    private static Node? CloneList(Node? head)
    {
        if (head == null) return null;

        var newHead = new Node(head.Value);
        var oldCurr = head.Next;
        var newCurr = newHead;

        while (oldCurr != null)
        {
            newCurr.Next = new Node(oldCurr.Value);
            newCurr = newCurr.Next;
            oldCurr = oldCurr.Next;
        }
        return newHead;
    }
}