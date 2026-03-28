using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(e => e.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(e => e.Value).ToList();
    
    
    public virtual void Add(TKey key, TValue value)
    {
        TNode? parent = null;
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { current.Value = value; return; }
            parent = current;
            current = cmp < 0 ? current.Left : current.Right;
        }

        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;
        if (parent == null)
        {
            Root = newNode;
        }
        else if (Comparer.Compare(key, parent.Key) < 0)
        {
            parent.Left = newNode;
        }
        else
        {
            parent.Right = newNode;
        }
        Count++;
        OnNodeAdded(newNode);
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left == null)
        {
            TNode? parent = node.Parent;
            TNode? child = node.Right;
            Transplant(node, node.Right);
            OnNodeRemoved(parent, child);
        }
        else if (node.Right == null)
        {
            TNode? parent = node.Parent;
            TNode? child = node.Left;
            Transplant(node, node.Left);
            OnNodeRemoved(parent, child);
        }
        else
        {
            // Find in-order successor (minimum of right subtree)
            TNode successor = node.Right;
            while (successor.Left != null) successor = successor.Left;

            TNode? x = successor.Right;
            TNode? xParent;

            if (successor.Parent != node)
            {
                xParent = successor.Parent;
                Transplant(successor, successor.Right);
                successor.Right = node.Right;
                successor.Right.Parent = successor;
            }
            else
            {
                xParent = successor;
            }

            Transplant(node, successor);
            successor.Left = node.Left;
            successor.Left.Parent = successor;

            OnNodeRemoved(xParent, x);
        }
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        TNode y = x.Right!;
        x.Right = y.Left;
        if (y.Left != null) y.Left.Parent = x;
        y.Parent = x.Parent;
        if (x.Parent == null) Root = y;
        else if (x.IsLeftChild) x.Parent.Left = y;
        else x.Parent.Right = y;
        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        TNode x = y.Left!;
        y.Left = x.Right;
        if (x.Right != null) x.Right.Parent = y;
        x.Parent = y.Parent;
        if (y.Parent == null) Root = x;
        else if (y.IsLeftChild) y.Parent.Left = x;
        else y.Parent.Right = x;
        x.Right = y;
        y.Parent = x;
    }
    
    protected void RotateBigLeft(TNode x)
    {
        RotateRight(x.Right!);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        RotateLeft(y.Left!);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        RotateLeft(x.Parent!);
        RotateLeft(x.Parent!);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        RotateRight(y.Parent!);
        RotateRight(y.Parent!);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() =>
        new TreeIterator(Root, TraversalStrategy.InOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() =>
        new TreeIterator(Root, TraversalStrategy.PreOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() =>
        new TreeIterator(Root, TraversalStrategy.PostOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() =>
        new TreeIterator(Root, TraversalStrategy.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() =>
        new TreeIterator(Root, TraversalStrategy.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() =>
        new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    /// <summary>
    /// Внутренний класс-итератор.
    /// Реализует паттерн Iterator вручную без yield return и без Stack —
    /// перемещение осуществляется по указателям Parent/Left/Right от текущего узла.
    /// </summary>
    private struct TreeIterator :
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TraversalStrategy _strategy;
        private readonly TNode? _root;
        private TNode? _current;
        private int _depth;
        private bool _started;
        private bool _finished;

        internal TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _current = null;
            _depth = 0;
            _started = false;
            _finished = false;
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        public TreeEntry<TKey, TValue> Current
        {
            get
            {
                if (_current == null) throw new InvalidOperationException();
                return new TreeEntry<TKey, TValue>(_current.Key, _current.Value, _depth);
            }
        }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_finished) return false;

            if (!_started)
            {
                _started = true;
                if (_root == null) { _finished = true; return false; }

                switch (_strategy)
                {
                    case TraversalStrategy.InOrder:
                        (_current, _depth) = GoLeftMost(_root, 0);
                        break;
                    case TraversalStrategy.InOrderReverse:
                        (_current, _depth) = GoRightMost(_root, 0);
                        break;
                    case TraversalStrategy.PreOrder:
                        _current = _root; _depth = 0;
                        break;
                    case TraversalStrategy.PostOrder:
                        (_current, _depth) = PostOrderFirst(_root, 0);
                        break;
                    case TraversalStrategy.PreOrderReverse:
                        (_current, _depth) = PreOrderReverseFirst(_root, 0);
                        break;
                    case TraversalStrategy.PostOrderReverse:
                        _current = _root; _depth = 0;
                        break;
                }
                return true;
            }

            TNode? next;
            int nextDepth;

            switch (_strategy)
            {
                case TraversalStrategy.InOrder:
                    (next, nextDepth) = InOrderNext(_current!, _depth);
                    break;
                case TraversalStrategy.InOrderReverse:
                    (next, nextDepth) = InOrderReverseNext(_current!, _depth);
                    break;
                case TraversalStrategy.PreOrder:
                    (next, nextDepth) = PreOrderNext(_current!, _depth);
                    break;
                case TraversalStrategy.PostOrder:
                    (next, nextDepth) = PostOrderNext(_current!, _depth);
                    break;
                case TraversalStrategy.PreOrderReverse:
                    (next, nextDepth) = PreOrderReverseNext(_current!, _depth);
                    break;
                case TraversalStrategy.PostOrderReverse:
                    (next, nextDepth) = PostOrderReverseNext(_current!, _depth);
                    break;
                default:
                    next = null; nextDepth = 0;
                    break;
            }

            if (next == null) { _finished = true; return false; }
            _current = next;
            _depth = nextDepth;
            return true;
        }

        // ── helpers ─────────────────────────────────────────────────────────────

        private static (TNode node, int depth) GoLeftMost(TNode node, int depth)
        {
            while (node.Left != null) { node = node.Left; depth++; }
            return (node, depth);
        }

        private static (TNode node, int depth) GoRightMost(TNode node, int depth)
        {
            while (node.Right != null) { node = node.Right; depth++; }
            return (node, depth);
        }

        // InOrder: Left → Root → Right
        private static (TNode? node, int depth) InOrderNext(TNode current, int depth)
        {
            if (current.Right != null)
                return GoLeftMost(current.Right, depth + 1);

            while (current.Parent != null && current.IsRightChild)
            {
                current = current.Parent;
                depth--;
            }
            if (current.Parent == null) return (null, 0);
            return (current.Parent, depth - 1);
        }

        // InOrderReverse: Right → Root → Left
        private static (TNode? node, int depth) InOrderReverseNext(TNode current, int depth)
        {
            if (current.Left != null)
                return GoRightMost(current.Left, depth + 1);

            while (current.Parent != null && current.IsLeftChild)
            {
                current = current.Parent;
                depth--;
            }
            if (current.Parent == null) return (null, 0);
            return (current.Parent, depth - 1);
        }

        // PreOrder: Root → Left → Right
        private static (TNode? node, int depth) PreOrderNext(TNode current, int depth)
        {
            if (current.Left != null) return (current.Left, depth + 1);
            if (current.Right != null) return (current.Right, depth + 1);

            while (current.Parent != null)
            {
                TNode parent = current.Parent;
                depth--;
                if (current.IsLeftChild && parent.Right != null)
                    return (parent.Right, depth + 1);
                current = parent;
            }
            return (null, 0);
        }

        // PostOrder first: deepest node preferring Left then Right
        private static (TNode node, int depth) PostOrderFirst(TNode node, int depth)
        {
            while (true)
            {
                if (node.Left != null) { node = node.Left; depth++; }
                else if (node.Right != null) { node = node.Right; depth++; }
                else break;
            }
            return (node, depth);
        }

        // PostOrder: Left → Right → Root
        private static (TNode? node, int depth) PostOrderNext(TNode current, int depth)
        {
            if (current.Parent == null) return (null, 0);

            TNode parent = current.Parent;
            if (current.IsRightChild) return (parent, depth - 1);

            // current is left child
            if (parent.Right != null)
                return PostOrderFirst(parent.Right, depth); // sibling is at same depth

            return (parent, depth - 1);
        }

        // PreOrderReverse first: deepest node preferring Right then Left
        private static (TNode node, int depth) PreOrderReverseFirst(TNode node, int depth)
        {
            while (true)
            {
                if (node.Right != null) { node = node.Right; depth++; }
                else if (node.Left != null) { node = node.Left; depth++; }
                else break;
            }
            return (node, depth);
        }

        // PreOrderReverse: Right subtree → Left subtree → Root
        private static (TNode? node, int depth) PreOrderReverseNext(TNode current, int depth)
        {
            if (current.Parent == null) return (null, 0);

            TNode parent = current.Parent;
            if (current.IsLeftChild) return (parent, depth - 1);

            // current is right child
            if (parent.Left != null)
                return PreOrderReverseFirst(parent.Left, depth); // sibling is at same depth

            return (parent, depth - 1);
        }

        // PostOrderReverse: Root → Right → Left
        private static (TNode? node, int depth) PostOrderReverseNext(TNode current, int depth)
        {
            if (current.Right != null) return (current.Right, depth + 1);
            if (current.Left != null) return (current.Left, depth + 1);

            while (current.Parent != null)
            {
                TNode parent = current.Parent;
                depth--;
                if (current.IsRightChild && parent.Left != null)
                    return (parent.Left, depth + 1);
                current = parent;
            }
            return (null, 0);
        }

        public void Reset()
        {
            _current = null;
            _depth = 0;
            _started = false;
            _finished = false;
        }

        public void Dispose()
        {
            // No managed resources to release
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        => InOrder().Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value)).GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}