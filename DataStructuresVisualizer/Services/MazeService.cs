namespace DataStructuresVisualizer.Services;

using DataStructuresVisualizer.Models;

public static class MazeGenerator
{
    public static MazeGrid GenerateRandom(Random? random = null)
    {
        random ??= Random.Shared;

        var widthCells = random.Next(9, 15);
        var heightCells = random.Next(6, 11);
        var width = widthCells * 2 + 1;
        var height = heightCells * 2 + 1;
        var cells = new char[width, height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                cells[x, y] = '#';
            }
        }

        var visited = new bool[widthCells, heightCells];
        var stack = new Stack<(int x, int y)>();

        void CarveCell(int x, int y)
        {
            cells[x * 2 + 1, y * 2 + 1] = '.';
        }

        void CarveWallBetween((int x, int y) a, (int x, int y) b)
        {
            var wallX = a.x + b.x + 1;
            var wallY = a.y + b.y + 1;
            cells[wallX, wallY] = '.';
        }

        CarveCell(0, 0);
        visited[0, 0] = true;
        stack.Push((0, 0));

        int[] dx = [1, -1, 0, 0];
        int[] dy = [0, 0, 1, -1];

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var neighbors = new List<(int x, int y)>();

            for (var i = 0; i < dx.Length; i++)
            {
                var nx = current.x + dx[i];
                var ny = current.y + dy[i];

                if (nx >= 0 && ny >= 0 && nx < widthCells && ny < heightCells && !visited[nx, ny])
                {
                    neighbors.Add((nx, ny));
                }
            }

            if (neighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            var next = neighbors[random.Next(neighbors.Count)];
            visited[next.x, next.y] = true;
            CarveCell(next.x, next.y);
            CarveWallBetween(current, next);
            stack.Push(next);
        }

        // Небольшое количество дополнительных проходов делает лабиринт живее.
        for (var x = 1; x < width - 1; x++)
        {
            for (var y = 1; y < height - 1; y++)
            {
                if (cells[x, y] == '#' && random.NextDouble() < 0.09)
                {
                    cells[x, y] = '.';
                }
            }
        }

        cells[1, 1] = 'S';
        cells[width - 2, height - 2] = 'E';

        return new MazeGrid(cells, new MazePoint(1, 1), new MazePoint(width - 2, height - 2));
    }
}

public static class MazeParser
{
    public static bool TryParse(string text, out MazeGrid? grid, out string error)
    {
        grid = null;
        error = string.Empty;

        var sourceLines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd())
            .Where(line => line.Length > 0)
            .ToArray();

        if (sourceLines.Length == 0)
        {
            error = "Лабиринт пуст.";
            return false;
        }

        var width = sourceLines.Max(line => line.Length);
        var height = sourceLines.Length;
        var cells = new char[width, height];
        var starts = new List<MazePoint>();
        var ends = new List<MazePoint>();

        for (var y = 0; y < height; y++)
        {
            var line = sourceLines[y];

            for (var x = 0; x < width; x++)
            {
                var value = x < line.Length ? line[x] : '#';

                switch (value)
                {
                    case '#':
                        cells[x, y] = '#';
                        break;
                    case '.':
                        cells[x, y] = '.';
                        break;
                    case 'S':
                    case 's':
                        cells[x, y] = 'S';
                        starts.Add(new MazePoint(x, y));
                        break;
                    case 'E':
                    case 'e':
                        cells[x, y] = 'E';
                        ends.Add(new MazePoint(x, y));
                        break;
                    default:
                        error = $"Недопустимый символ «{value}». Разрешены #, ., S и E.";
                        return false;
                }
            }
        }

        if (starts.Count != 1)
        {
            error = "Нужен ровно один символ S.";
            return false;
        }

        if (ends.Count != 1)
        {
            error = "Нужен ровно один символ E.";
            return false;
        }

        grid = new MazeGrid(cells, starts[0], ends[0]);
        return true;
    }
}

public static class MazeSolver
{
    public static IReadOnlyList<MazeFrame> Solve(MazeGrid grid)
    {
        var stack = new List<MazePoint> { grid.Start };
        var visited = new HashSet<MazePoint> { grid.Start };
        var parent = new Dictionary<MazePoint, MazePoint>();
        var frames = new List<MazeFrame>();

        MazePoint[] Clone() => stack.ToArray();

        frames.Add(new MazeFrame(
            MazeFrameType.PushStart,
            grid.Start,
            Clone(),
            "Старт добавлен в стек."));

        int[] dx = [1, -1, 0, 0];
        int[] dy = [0, 0, 1, -1];

        while (stack.Count > 0)
        {
            var current = stack[^1];

            frames.Add(new MazeFrame(
                MazeFrameType.Explore,
                current,
                Clone(),
                $"Вершина стека: ({current.X}, {current.Y})."));

            if (current == grid.Goal)
            {
                frames.Add(new MazeFrame(
                    MazeFrameType.Found,
                    current,
                    Clone(),
                    "Выход найден! Восстанавливаем путь."));

                var path = BuildPath(parent, grid.Goal, grid.Start);

                foreach (var point in path)
                {
                    frames.Add(new MazeFrame(
                        MazeFrameType.FinalPathStep,
                        point,
                        Clone(),
                        "Путь к выходу."));
                }

                return frames;
            }

            var candidates = new List<MazePoint>();

            for (var i = 0; i < dx.Length; i++)
            {
                var next = new MazePoint(current.X + dx[i], current.Y + dy[i]);

                if (grid.IsOpen(next) && !visited.Contains(next))
                {
                    candidates.Add(next);
                }
            }

            if (candidates.Count == 0)
            {
                frames.Add(new MazeFrame(
                    MazeFrameType.PopCurrent,
                    current,
                    Clone(),
                    "Тупик: возвращаемся назад через Pop."));

                stack.RemoveAt(stack.Count - 1);

                frames.Add(new MazeFrame(
                    MazeFrameType.DeadEnd,
                    current,
                    Clone(),
                    "Координата удалена из стека."));

                continue;
            }

            Shuffle(candidates, Random.Shared);
            var chosen = candidates[0];
            visited.Add(chosen);
            parent[chosen] = current;
            stack.Add(chosen);

            frames.Add(new MazeFrame(
                MazeFrameType.VisitNeighbor,
                chosen,
                Clone(),
                $"Push: ({chosen.X}, {chosen.Y})."));
        }

        frames.Add(new MazeFrame(
            MazeFrameType.NoPath,
            grid.Start,
            [],
            "Выход недостижим."));

        return frames;
    }

    private static IReadOnlyList<MazePoint> BuildPath(
        Dictionary<MazePoint, MazePoint> parent,
        MazePoint goal,
        MazePoint start)
    {
        var path = new List<MazePoint> { goal };
        var current = goal;

        while (parent.TryGetValue(current, out var previous) && current != start)
        {
            path.Add(previous);
            current = previous;
        }

        path.Reverse();
        return path;
    }

    private static void Shuffle<T>(IList<T> items, Random random)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
