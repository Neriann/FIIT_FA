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
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TraversalStrategy _strategy;
        private readonly TNode? _root;
        private Stack<(TNode node, bool visitNow, int depth)>? _stack;
        private TNode? _current;
        private int _currentDepth;
        private bool _started;

        internal TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _stack = null;
            _current = null;
            _currentDepth = 0;
            _started = false;
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current
        {
            get
            {
                if (_current == null) throw new InvalidOperationException();
                return new TreeEntry<TKey, TValue>(_current.Key, _current.Value, _currentDepth);
            }
        }

        object IEnumerator.Current => Current;
        
        public bool MoveNext()
        {
            if (!_started)
            {
                _stack = new Stack<(TNode, bool, int)>();
                if (_root != null) _stack.Push((_root, false, 0));
                _started = true;
            }

            while (_stack!.Count > 0)
            {
                var (node, visitNow, depth) = _stack.Pop();

                switch (_strategy)
                {
                    case TraversalStrategy.InOrder:
                        // Left → Root → Right
                        if (visitNow) { _current = node; _currentDepth = depth; return true; }
                        if (node.Right != null) _stack.Push((node.Right, false, depth + 1));
                        _stack.Push((node, true, depth));
                        if (node.Left != null) _stack.Push((node.Left, false, depth + 1));
                        break;

                    case TraversalStrategy.PreOrder:
                        // Root → Left → Right (emit on first encounter)
                        if (node.Right != null) _stack.Push((node.Right, false, depth + 1));
                        if (node.Left != null) _stack.Push((node.Left, false, depth + 1));
                        _current = node; _currentDepth = depth; return true;

                    case TraversalStrategy.PostOrder:
                        // Left → Right → Root
                        if (visitNow) { _current = node; _currentDepth = depth; return true; }
                        _stack.Push((node, true, depth));
                        if (node.Right != null) _stack.Push((node.Right, false, depth + 1));
                        if (node.Left != null) _stack.Push((node.Left, false, depth + 1));
                        break;

                    case TraversalStrategy.InOrderReverse:
                        // Right → Root → Left
                        if (visitNow) { _current = node; _currentDepth = depth; return true; }
                        if (node.Left != null) _stack.Push((node.Left, false, depth + 1));
                        _stack.Push((node, true, depth));
                        if (node.Right != null) _stack.Push((node.Right, false, depth + 1));
                        break;

                    case TraversalStrategy.PreOrderReverse:
                        // Emits: Right subtree → Left subtree → Root  (= reverse of Root→Left→Right)
                        // Root is deferred (visitNow=true), right child is pushed under left so right is popped first
                        if (visitNow) { _current = node; _currentDepth = depth; return true; }
                        _stack.Push((node, true, depth));
                        if (node.Left != null) _stack.Push((node.Left, false, depth + 1));
                        if (node.Right != null) _stack.Push((node.Right, false, depth + 1));
                        break;

                    case TraversalStrategy.PostOrderReverse:
                        // Emits: Root → Right subtree → Left subtree  (= reverse of Left→Right→Root)
                        // Left is pushed first (bottom), right on top so right is popped and visited first
                        if (node.Left != null) _stack.Push((node.Left, false, depth + 1));
                        if (node.Right != null) _stack.Push((node.Right, false, depth + 1));
                        _current = node; _currentDepth = depth; return true;
                }
            }
            return false;
        }
        
        public void Reset()
        {
            _stack = null;
            _current = null;
            _currentDepth = 0;
            _started = false;
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