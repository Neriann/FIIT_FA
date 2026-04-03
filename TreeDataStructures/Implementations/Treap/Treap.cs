using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(
        TreapNode<TKey, TValue>? root, TKey key, bool includeEqualToLeft)
    {
        if (root == null) return (null, null);

        int cmp = Comparer.Compare(root.Key, key);
        bool goLeft = cmp < 0 || (includeEqualToLeft && cmp == 0);
        if (goLeft)
        {
            var (t1, t2) = Split(root.Right, key, includeEqualToLeft);

            root.Right = t1;
            t1?.Parent = root;
            return (root, t2);
        }
        else
        {
            var (t1, t2) = Split(root.Left, key, includeEqualToLeft);

            root.Left = t2;
            t2?.Parent = root;
            return (t1, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null) return right;
        if (right == null) return left;

        TreapNode<TKey, TValue>? child;
        if (left.Priority > right.Priority)
        {
            child = Merge(left.Right, right);
            
            left.Right = child;
            child?.Parent = left;
            return left;
        }
        child = Merge(left, right.Left);
        
        right.Left = child;
        child?.Parent = right;
        return right;
    }


    public override void Add(TKey key, TValue value)
    {
        TreapNode<TKey, TValue>? existing = FindNode(key);
        if (existing != null)
        {
            existing.Value = value;
            return;
        }

        TreapNode<TKey, TValue>? node = CreateNode(key, value);
        var (t1, t2) = Split(Root, key, true);

        Root = Merge(Merge(t1, node), t2);
        Root!.Parent = null;
        ++Count;
    }

    protected override void RemoveNode(TreapNode<TKey, TValue> node)
    {
        var (le, gt) = Split(Root, node.Key, true);
        var (lt, eq) = Split(le, node.Key, false);

        Root = Merge(lt, gt);
        Root?.Parent = null;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value) =>
        new TreapNode<TKey, TValue>(key, value);
}