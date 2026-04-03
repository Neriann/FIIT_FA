using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {}
    
    protected override void RemoveNode(BstNode<TKey, TValue> node)
    {
        Splay(node);

        BstNode<TKey, TValue>? leftSubtree = Root?.Left;
        BstNode<TKey, TValue>? rightSubtree = Root?.Right;

        leftSubtree?.Parent = null;
        rightSubtree?.Parent = null;

        Root?.Left = null;
        Root?.Right = null;

        if (leftSubtree == null)
        {
            Root = rightSubtree;
            return;
        }

        Root = leftSubtree;
        int depth = 0;
        BstNode<TKey, TValue> maxElement = GetMaximumElement(leftSubtree, ref depth);
        
        Splay(maxElement);
        
        Root.Right = rightSubtree;
        rightSubtree?.Parent = Root;
    }

    public override bool ContainsKey(TKey key)
    {
        return TryGetValue(key, out _);
    }

    private BstNode<TKey, TValue>? FindNode(TKey key, out BstNode<TKey, TValue>? prev)
    {
        prev = null;
        
        BstNode<TKey, TValue>? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                return current;
            }
            prev = current;
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        BstNode<TKey, TValue>? target = FindNode(key, out BstNode<TKey, TValue>? prev);
        if (target != null)
        {
            value = target.Value;
            Splay(target);
            return true;
        }
        value = default;
        Splay(prev);
        return false;
    }

    private void Splay(BstNode<TKey, TValue>? node)
    {
        while (node?.Parent != null)
        {
            if (node.Parent.Parent == null)
            {
                if (node.IsLeftChild) RotateRight(node.Parent);
                else  RotateLeft(node.Parent);
            }
            // LL
            else if (node.Parent.IsLeftChild && node.IsLeftChild)
            {
                RotateDoubleRight(node.Parent.Parent);
            } 
            // LR
            else if (node.Parent.IsLeftChild && node.IsRightChild)
            {
                RotateBigRight(node.Parent.Parent);
            }
            // RR
            else if (node.Parent.IsRightChild && node.IsRightChild)
            {
                RotateDoubleLeft(node.Parent.Parent);
            }
            // RL
            else
            {
                RotateBigLeft(node.Parent.Parent);
            }
        }
    }
    
}
