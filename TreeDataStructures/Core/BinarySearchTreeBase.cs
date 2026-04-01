using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null)
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;

    public IComparer<TKey> Comparer { get; protected set; } =
        comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }

    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(e => e.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(e => e.Value).ToList();

    public virtual void Add(TKey key, TValue value)
    {
        TNode newNode = CreateNode(key, value);

        if (Root == null)
        {
            Root = newNode;
            Count++;
            OnNodeAdded(newNode);
            return;
        }

        TNode current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                current.Value = value; // updating value for existing key
                return;
            }
            else if (cmp < 0)
            {
                if (current.Left == null)
                {
                    current.Left = newNode;
                    newNode.Parent = current;
                    ++Count;
                    OnNodeAdded(newNode);
                    return;
                }

                current = current.Left;
            }
            else
            {
                if (current.Right == null)
                {
                    current.Right = newNode;
                    newNode.Parent = current;
                    ++Count;
                    OnNodeAdded(newNode);
                    return;
                }

                current = current.Right;
            }
        }
    }


    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null)
        {
            return false;
        }

        RemoveNode(node);
        this.Count--;
        return true;
    }


    private static TNode GetMinimumElement(TNode node, ref int depth)
    {
        while (node.Left != null)
        {
            node = node.Left;
            ++depth;
        }

        return node;
    }

    private static TNode GetMaximumElement(TNode node, ref int depth)
    {
        while (node.Right != null)
        {
            node = node.Right;
            ++depth;
        }

        return node;
    }

    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left == null)
        {
            Transplant(node, node.Right);
        }
        else if (node.Right == null)
        {
            Transplant(node, node.Left);
        }
        else
        {
            int depth = 0;
            TNode successor = GetMinimumElement(node.Right, ref depth);
            if (successor.Parent != node)
            {
                Transplant(successor, successor.Right);
                Transplant(node, successor);

                successor.Right = node.Right;
                successor.Right.Parent = successor;

                successor.Left = node.Left;
                successor.Left.Parent = successor;
            }
            else
            {
                Transplant(node, successor);

                successor.Left = node.Left;
                successor.Left.Parent = successor;
            }

            if (node == Root) Root = successor;
        }

        OnNodeRemoved(node.Parent, node);
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
    protected virtual void OnNodeAdded(TNode newNode)
    {
    }

    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child)
    {
    }

    #endregion


    #region Helpers

    protected abstract TNode CreateNode(TKey key, TValue value);


    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                return current;
            }

            current = cmp < 0 ? current.Left : current.Right;
        }

        return null;
    }

    protected void RotateLeft(TNode p)
    {
        TNode? y = p.Right;
        if (y == null) return;

        p.Right = y.Left;
        y.Left?.Parent = p;

        Transplant(p, y);

        y.Left = p;
        p.Parent = y;
    }

    protected void RotateRight(TNode p)
    {
        TNode? x = p.Left;
        if (x == null) return;

        p.Left = x.Right;
        x.Right?.Parent = p;

        Transplant(p, x);

        x.Right = p;
        p.Parent = x;
    }

    protected void RotateBigLeft(TNode p)
    {
        if (p.Right == null) return;

        RotateRight(p.Right);
        RotateLeft(p);
    }

    protected void RotateBigRight(TNode p)
    {
        if (p.Left == null) return;

        RotateLeft(p.Left);
        RotateRight(p);
    }

    protected void RotateDoubleLeft(TNode p)
    {
        if (p.Right?.Right == null) return;

        RotateLeft(p);
        RotateLeft(p.Parent!);
    }

    protected void RotateDoubleRight(TNode p)
    {
        if (p.Left?.Left == null) return;

        RotateRight(p);
        RotateRight(p.Parent!);
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

    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => InOrderTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> InOrderTraversal(TNode? node) =>
        new TreeIterator(node, TraversalStrategy.InOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => PreOrderTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> PreOrderTraversal(TNode? node) =>
        new TreeIterator(node, TraversalStrategy.PreOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => PostOrderTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> PostOrderTraversal(TNode? node) =>
        new TreeIterator(node, TraversalStrategy.PostOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => InOrderReverseTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> InOrderReverseTraversal(TNode? node) =>
        new TreeIterator(node, TraversalStrategy.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => PreOrderReverseTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverseTraversal(TNode? node) =>
        new TreeIterator(node, TraversalStrategy.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => PostOrderReverseTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverseTraversal(TNode? node) =>
        new TreeIterator(node, TraversalStrategy.PostOrderReverse);

    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator :
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        // probably add something here
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy; // or make it template parameter?

        private TNode? _curr;
        private int _depth;
        private bool _started;

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        public TreeEntry<TKey, TValue> Current => new TreeEntry<TKey, TValue>(_curr!.Key, _curr.Value, _depth);
        object IEnumerator.Current => Current;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            Reset();
        }

        public bool MoveNext()
        {
            if (!_started)
            {
                _started = true;
                _curr = GetFirstElement();
                return _curr != null;
            }

            if (_curr == null) return false;
            _curr = GetNextElement();
            return _curr != null;
        }

        public void Reset()
        {
            _started = false;
            _curr = null;
            _depth = 0;
        }


        public void Dispose()
        {
        }

        private TNode? GetFirstElement()
        {
            if (_root == null) return null;

            return _strategy switch
            {
                TraversalStrategy.InOrder or TraversalStrategy.PostOrder => GetMinimumElement(_root, ref _depth),
                TraversalStrategy.InOrderReverse or TraversalStrategy.PreOrderReverse => GetMaximumElement(_root,
                    ref _depth),
                _ => _root
            };
        }

        private TNode? GetNextElement()
        {
            if (_curr == null) return null;

            switch (_strategy)
            {
                case TraversalStrategy.InOrder:
                    if (_curr.Right != null)
                    {
                        _curr = GetMinimumElement(_curr.Right, ref _depth);
                        break;
                    }

                    while (_curr.Parent != null && !_curr.IsLeftChild)
                    {
                        _curr = _curr.Parent;
                        --_depth;
                    }

                    _curr = _curr.Parent;
                    --_depth;
                    break;

                case TraversalStrategy.PreOrder:
                    if (_curr.Left != null)
                    {
                        _curr = _curr.Left;
                        ++_depth;
                        break;
                    }

                    if (_curr.Right != null)
                    {
                        _curr = _curr.Right;
                        ++_depth;
                        break;
                    }

                    while (_curr.Parent != null && (_curr.IsRightChild || _curr.Parent.Right == null))
                    {
                        _curr = _curr.Parent;
                        --_depth;
                    }

                    _curr = _curr.Parent?.Right;
                    break;

                case TraversalStrategy.PostOrder:
                    if (_curr.Parent == null) return null;

                    if (_curr.IsLeftChild && _curr.Parent.Right != null)
                    {
                        _curr = GetMinimumElement(_curr.Parent.Right, ref _depth);
                        break;
                    }

                    _curr = _curr.Parent;
                    --_depth;
                    break;

                case TraversalStrategy.InOrderReverse:
                    if (_curr.Left != null)
                    {
                        _curr = GetMaximumElement(_curr.Left, ref _depth);
                        break;
                    }

                    while (_curr.Parent != null && !_curr.IsRightChild)
                    {
                        _curr = _curr.Parent;
                        --_depth;
                    }

                    _curr = _curr.Parent;
                    --_depth;
                    break;

                case TraversalStrategy.PreOrderReverse:
                    if (_curr.Parent == null) return null;

                    if (_curr.IsRightChild && _curr.Parent.Left != null)
                    {
                        _curr = GetMaximumElement(_curr.Parent.Left, ref _depth);
                        break;
                    }

                    _curr = _curr.Parent;
                    --_depth;
                    break;

                case TraversalStrategy.PostOrderReverse:
                    if (_curr.Right != null)
                    {
                        _curr = _curr.Right;
                        ++_depth;
                        break;
                    }

                    if (_curr.Left != null)
                    {
                        _curr = _curr.Left;
                        ++_depth;
                        break;
                    }

                    while (_curr.Parent != null && (_curr.IsLeftChild || _curr.Parent.Left == null))
                    {
                        _curr = _curr.Parent;
                        --_depth;
                    }

                    _curr = _curr.Parent?.Left;
                    break;
            }

            return _curr;
        }
    }


    private enum TraversalStrategy
    {
        InOrder,
        PreOrder,
        PostOrder,
        InOrderReverse,
        PreOrderReverse,
        PostOrderReverse
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return InOrder()
            .Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        Root = null;
        Count = 0;
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array); // check for null array

        if (arrayIndex < 0 || arrayIndex > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("Not enough space in the target array.", nameof(array));
        }

        foreach (var entry in InOrder())
        {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}