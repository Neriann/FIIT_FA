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
            if (cmp == 0)
            {
                current.Value = value;
                return;
            }
            parent = current;
            current = cmp < 0 ? current.Left : current.Right;
        }

        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;

        if (parent == null)
            Root = newNode;
        else if (Comparer.Compare(key, parent.Key) < 0)
            parent.Left = newNode;
        else
            parent.Right = newNode;

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
            Transplant(node, child);
            OnNodeRemoved(parent, child);
        }
        else if (node.Right == null)
        {
            TNode? parent = node.Parent;
            TNode? child = node.Left;
            Transplant(node, child);
            OnNodeRemoved(parent, child);
        }
        else
        {
            // Find in-order successor (leftmost node in right subtree)
            TNode successor = node.Right;
            while (successor.Left != null)
                successor = successor.Left;

            TNode? fixupParent;
            TNode? fixupChild = successor.Right;

            if (successor.Parent == node)
            {
                fixupParent = successor;
                Transplant(node, successor);
                successor.Left = node.Left;
                successor.Left!.Parent = successor;
            }
            else
            {
                fixupParent = successor.Parent;
                Transplant(successor, successor.Right);
                successor.Right = node.Right;
                successor.Right!.Parent = successor;
                Transplant(node, successor);
                successor.Left = node.Left;
                successor.Left!.Parent = successor;
            }

            OnNodeRemoved(fixupParent, fixupChild);
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
        if (y.Left != null)
            y.Left.Parent = x;
        y.Parent = x.Parent;
        if (x.Parent == null)
            Root = y;
        else if (x.IsLeftChild)
            x.Parent.Left = y;
        else
            x.Parent.Right = y;
        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        TNode x = y.Left!;
        y.Left = x.Right;
        if (x.Right != null)
            x.Right.Parent = y;
        x.Parent = y.Parent;
        if (y.Parent == null)
            Root = x;
        else if (y.IsLeftChild)
            y.Parent.Left = x;
        else
            y.Parent.Right = x;
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
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => InOrderTraversal(Root, 0);
    
    private IEnumerable<TreeEntry<TKey, TValue>> InOrderTraversal(TNode? node, int depth)
    {
        if (node == null) { yield break; }
        foreach (var e in InOrderTraversal(node.Left, depth + 1)) yield return e;
        yield return new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
        foreach (var e in InOrderTraversal(node.Right, depth + 1)) yield return e;
    }
    
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => PreOrderTraversal(Root, 0);

    private IEnumerable<TreeEntry<TKey, TValue>> PreOrderTraversal(TNode? node, int depth)
    {
        if (node == null) yield break;
        yield return new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
        foreach (var e in PreOrderTraversal(node.Left, depth + 1)) yield return e;
        foreach (var e in PreOrderTraversal(node.Right, depth + 1)) yield return e;
    }

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => PostOrderTraversal(Root, 0);

    private IEnumerable<TreeEntry<TKey, TValue>> PostOrderTraversal(TNode? node, int depth)
    {
        if (node == null) yield break;
        foreach (var e in PostOrderTraversal(node.Left, depth + 1)) yield return e;
        foreach (var e in PostOrderTraversal(node.Right, depth + 1)) yield return e;
        yield return new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
    }

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => InOrderReverseTraversal(Root, 0);

    private IEnumerable<TreeEntry<TKey, TValue>> InOrderReverseTraversal(TNode? node, int depth)
    {
        if (node == null) yield break;
        foreach (var e in InOrderReverseTraversal(node.Right, depth + 1)) yield return e;
        yield return new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
        foreach (var e in InOrderReverseTraversal(node.Left, depth + 1)) yield return e;
    }

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => PreOrderReverseTraversal(Root, 0);

    private IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverseTraversal(TNode? node, int depth)
    {
        if (node == null) yield break;
        foreach (var e in PreOrderReverseTraversal(node.Right, depth + 1)) yield return e;
        foreach (var e in PreOrderReverseTraversal(node.Left, depth + 1)) yield return e;
        yield return new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
    }

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => PostOrderReverseTraversal(Root, 0);

    private IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverseTraversal(TNode? node, int depth)
    {
        if (node == null) yield break;
        yield return new TreeEntry<TKey, TValue>(node.Key, node.Value, depth);
        foreach (var e in PostOrderReverseTraversal(node.Right, depth + 1)) yield return e;
        foreach (var e in PostOrderReverseTraversal(node.Left, depth + 1)) yield return e;
    }
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        // probably add something here
        private readonly TraversalStrategy _strategy; // or make it template parameter?
        
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current => throw new NotImplementedException();
        object IEnumerator.Current => Current;
        
        
        public bool MoveNext()
        {
            if (_strategy == TraversalStrategy.InOrder)
            {
                throw new NotImplementedException();
            }
            throw new NotImplementedException("Strategy not implemented");
        }
        
        public void Reset()
        {
            throw new NotImplementedException();
        }

        
        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var entry in InOrder())
            yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}