using System.Text;
using DataStructuresVisualizer.Models;

namespace DataStructuresVisualizer.Services;

public static class BinaryTreeService
{
    public static BinaryTreeBuildResult Build(string source)
    {
        var text = new string(source.Where(character => !char.IsWhiteSpace(character)).ToArray());

        if (text.Length == 0)
        {
            return new BinaryTreeBuildResult(null, "Строка пуста.", 0, 0);
        }

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '*' && !char.IsLetter(text[i]))
            {
                return new BinaryTreeBuildResult(
                    null,
                    $"Недопустимый символ «{text[i]}» в позиции {i + 1}.",
                    i,
                    text.Length);
            }
        }

        var index = 0;
        var root = BuildNode(text, ref index);

        if (root is null)
        {
            return new BinaryTreeBuildResult(
                null,
                "Корень дерева не может быть NULL.",
                index,
                text.Length);
        }

        if (index < text.Length)
        {
            return new BinaryTreeBuildResult(
                root,
                $"После построения дерева остались лишние символы: {text[index..]}.",
                index,
                text.Length);
        }

        return new BinaryTreeBuildResult(root, null, index, text.Length);
    }

    public static string GenerateRandom(Random? random = null)
    {
        random ??= Random.Shared;
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var nextLetter = 0;

        string Build(int depth)
        {
            if (depth >= 4 || (depth > 0 && random.NextDouble() < 0.32))
            {
                return "*";
            }

            var letter = alphabet[nextLetter++ % alphabet.Length];
            return letter + Build(depth + 1) + Build(depth + 1);
        }

        return Build(0);
    }

    public static (int Width, int Height) Layout(BinaryTreeNode root)
    {
        var leafIndex = 0;
        AssignLayout(root, 0, ref leafIndex);

        var minX = MinimumX(root);
        var maxX = MaximumX(root);
        var shift = 80 - minX;

        ApplyShift(root, shift);

        var maxY = MaximumY(root);
        return ((int)(maxX - minX + 220), (int)(maxY + 150));
    }

    public static int CountNodes(BinaryTreeNode? node) =>
        node is null ? 0 : 1 + CountNodes(node.Left) + CountNodes(node.Right);

    public static int CountNulls(BinaryTreeNode? node)
    {
        if (node is null)
        {
            return 1;
        }

        return CountNulls(node.Left) + CountNulls(node.Right);
    }

    public static int Height(BinaryTreeNode? node)
    {
        if (node is null)
        {
            return 0;
        }

        return 1 + Math.Max(Height(node.Left), Height(node.Right));
    }

    private static BinaryTreeNode? BuildNode(string text, ref int index)
    {
        if (index >= text.Length)
        {
            return null;
        }

        if (text[index] == '*')
        {
            index++;
            return null;
        }

        var node = new BinaryTreeNode
        {
            Value = text[index++]
        };

        node.Left = BuildNode(text, ref index);
        node.Right = BuildNode(text, ref index);
        return node;
    }

    private static void AssignLayout(BinaryTreeNode node, int depth, ref int leafIndex)
    {
        node.Y = 40 + depth * 180;

        if (node.Left is null && node.Right is null)
        {
            node.X = 80 + leafIndex * 180;
            leafIndex++;
            return;
        }

        if (node.Left is not null)
        {
            AssignLayout(node.Left, depth + 1, ref leafIndex);
        }

        if (node.Right is not null)
        {
            AssignLayout(node.Right, depth + 1, ref leafIndex);
        }

        node.X = node.Left is not null && node.Right is not null
            ? (node.Left.X + node.Right.X) / 2.0
            : node.Left?.X ?? node.Right?.X ?? node.X;
    }

    private static void ApplyShift(BinaryTreeNode node, double shift)
    {
        node.X += shift;

        if (node.Left is not null)
        {
            ApplyShift(node.Left, shift);
        }

        if (node.Right is not null)
        {
            ApplyShift(node.Right, shift);
        }
    }

    private static double MinimumX(BinaryTreeNode node)
    {
        var value = node.X;

        if (node.Left is not null)
        {
            value = Math.Min(value, MinimumX(node.Left));
        }

        if (node.Right is not null)
        {
            value = Math.Min(value, MinimumX(node.Right));
        }

        return value;
    }

    private static double MaximumX(BinaryTreeNode node)
    {
        var value = node.X;

        if (node.Left is not null)
        {
            value = Math.Max(value, MaximumX(node.Left));
        }

        if (node.Right is not null)
        {
            value = Math.Max(value, MaximumX(node.Right));
        }

        return value;
    }

    private static double MaximumY(BinaryTreeNode node)
    {
        var value = node.Y;

        if (node.Left is not null)
        {
            value = Math.Max(value, MaximumY(node.Left));
        }

        if (node.Right is not null)
        {
            value = Math.Max(value, MaximumY(node.Right));
        }

        return value;
    }
}
