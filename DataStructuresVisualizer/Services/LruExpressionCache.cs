namespace DataStructuresVisualizer.Services;

/// <summary>
/// Собственная LRU-хеш-таблица для кэширования уже вычисленных выражений.
/// Хеш-таблица решает поиск, а двусвязный список — порядок использования.
/// </summary>
public sealed class LruExpressionCache
{
    private sealed class Entry
    {
        public required string Key { get; init; }

        public double Value { get; set; }

        public int Hash { get; init; }

        public Entry? BucketNext { get; set; }

        public Entry? MoreRecent { get; set; }

        public Entry? LessRecent { get; set; }
    }

    private readonly Entry?[] _buckets;
    private Entry? _newest;
    private Entry? _oldest;

    public LruExpressionCache(int capacity = 96)
    {
        Capacity = Math.Max(4, capacity);
        _buckets = new Entry?[NextPrime(Capacity * 2)];
    }

    public int Capacity { get; }

    public int Count { get; private set; }

    public int Hits { get; private set; }

    public int Misses { get; private set; }

    public int Replacements { get; private set; }

    public bool TryGet(string key, out double value)
    {
        var entry = Find(key);

        if (entry is null)
        {
            Misses++;
            value = 0;
            return false;
        }

        MoveToFront(entry);
        Hits++;
        value = entry.Value;
        return true;
    }

    public void Put(string key, double value)
    {
        var existing = Find(key);

        if (existing is not null)
        {
            existing.Value = value;
            MoveToFront(existing);
            return;
        }

        var entry = new Entry
        {
            Key = key,
            Value = value,
            Hash = HashString(key)
        };

        AddToBucket(entry);
        AddToFront(entry);
        Count++;

        if (Count > Capacity)
        {
            RemoveOldest();
        }
    }

    public void Clear()
    {
        Array.Clear(_buckets);
        _newest = null;
        _oldest = null;
        Count = 0;
        Hits = 0;
        Misses = 0;
        Replacements = 0;
    }

    public string Describe() =>
        $"LRU: {Count}/{Capacity} · попаданий {Hits} · промахов {Misses} · вытеснений {Replacements}";

    private Entry? Find(string key)
    {
        var hash = HashString(key);
        var index = BucketIndex(hash);

        for (var entry = _buckets[index]; entry is not null; entry = entry.BucketNext)
        {
            if (entry.Hash == hash && string.Equals(entry.Key, key, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private void AddToBucket(Entry entry)
    {
        var index = BucketIndex(entry.Hash);
        entry.BucketNext = _buckets[index];
        _buckets[index] = entry;
    }

    private void RemoveFromBucket(Entry entry)
    {
        var index = BucketIndex(entry.Hash);
        var current = _buckets[index];
        Entry? previous = null;

        while (current is not null)
        {
            if (ReferenceEquals(current, entry))
            {
                if (previous is null)
                {
                    _buckets[index] = current.BucketNext;
                }
                else
                {
                    previous.BucketNext = current.BucketNext;
                }

                entry.BucketNext = null;
                return;
            }

            previous = current;
            current = current.BucketNext;
        }
    }

    private void AddToFront(Entry entry)
    {
        entry.MoreRecent = null;
        entry.LessRecent = _newest;

        if (_newest is not null)
        {
            _newest.MoreRecent = entry;
        }

        _newest = entry;

        if (_oldest is null)
        {
            _oldest = entry;
        }
    }

    private void MoveToFront(Entry entry)
    {
        if (ReferenceEquals(_newest, entry))
        {
            return;
        }

        RemoveFromRecency(entry);
        AddToFront(entry);
    }

    private void RemoveFromRecency(Entry entry)
    {
        if (entry.MoreRecent is not null)
        {
            entry.MoreRecent.LessRecent = entry.LessRecent;
        }
        else
        {
            _newest = entry.LessRecent;
        }

        if (entry.LessRecent is not null)
        {
            entry.LessRecent.MoreRecent = entry.MoreRecent;
        }
        else
        {
            _oldest = entry.MoreRecent;
        }

        entry.MoreRecent = null;
        entry.LessRecent = null;
    }

    private void RemoveOldest()
    {
        if (_oldest is null)
        {
            return;
        }

        var victim = _oldest;
        RemoveFromRecency(victim);
        RemoveFromBucket(victim);
        Count--;
        Replacements++;
    }

    private int BucketIndex(int hash)
    {
        var index = hash % _buckets.Length;
        return index < 0 ? -index : index;
    }

    private static int HashString(string key)
    {
        unchecked
        {
            var multiplier = (int)0x9E3779B1;
            var hash = (int)0x811C9DC5;

            foreach (var character in key)
            {
                hash = (hash ^ character) * multiplier;
            }

            return hash;
        }
    }

    private static int NextPrime(int value)
    {
        var candidate = Math.Max(7, value);

        while (!IsPrime(candidate))
        {
            candidate++;
        }

        return candidate;
    }

    private static bool IsPrime(int value)
    {
        if (value < 2)
        {
            return false;
        }

        if (value % 2 == 0)
        {
            return value == 2;
        }

        for (var divisor = 3; divisor * divisor <= value; divisor += 2)
        {
            if (value % divisor == 0)
            {
                return false;
            }
        }

        return true;
    }
}
