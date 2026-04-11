using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);

    private static RbNode<TKey, TValue>? Uncle(RbNode<TKey, TValue> node)
    {
        if (node.Parent?.Parent == null) return null;

        return node.Parent.IsLeftChild ? node.Parent.Parent.Right : node.Parent.Parent.Left;
    }

    private static RbNode<TKey, TValue>? Brother(RbNode<TKey, TValue>? p, RbNode<TKey, TValue>? y, bool xWasLeftChild)
    {
        if (p == null) return null;

        return y != null ? (y.IsLeftChild ? p.Right : p.Left) : (xWasLeftChild ? p.Right : p.Left);
    }
    
    private static bool IsBlack(RbNode<TKey, TValue>? node) => node == null || node.Color == RbColor.Black;

    private static bool IsRed(RbNode<TKey, TValue>? node) => !IsBlack(node);
    
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        if (newNode.Parent == null)
        {
            newNode.Color = RbColor.Black;
            return;
        }

        RbNode<TKey, TValue>? parent = newNode.Parent;
        if (IsBlack(parent)) return;

        // Guarantee that newNode.Parent.Parent is exists and is black
        RbNode<TKey, TValue>? uncle = Uncle(newNode);
        if (IsRed(uncle))
        {
            parent.Color = uncle!.Color = RbColor.Black;
            
            // Grandparent
            uncle.Parent!.Color = RbColor.Red;
            OnNodeAdded(uncle.Parent);
            return;
        }

        // Uncle is black or null
        if (parent.IsLeftChild)
        {
            if (newNode.IsRightChild)
            {
                RotateLeft(parent);

                parent = newNode;
            }
            parent.Color = RbColor.Black;
            parent.Parent!.Color = RbColor.Red;
            RotateRight(parent.Parent!);
        }
        else
        {
            if (newNode.IsLeftChild)
            {
                RotateRight(newNode.Parent);

                parent = newNode;
            }
            parent.Color = RbColor.Black;
            parent.Parent!.Color = RbColor.Red;
            RotateLeft(parent.Parent!);
        }

        Root?.Color = RbColor.Black;
    }


    protected override void RemoveNode(RbNode<TKey, TValue> node)
    {
        if (node is { Left: not null, Right: not null })
        {
            // Swap with successor
            int depth = 0;
            RbNode<TKey, TValue> successor = GetMinimumElement(node.Right, ref depth);
            node.Key = successor.Key;
            node.Value = successor.Value;
            RemoveNode(successor);
            return;
        }

        RbNode<TKey, TValue>? child = node.Left ?? node.Right;

        if (IsRed(node))
        {
            // Case0: Node - Red
            Transplant(node, child);
        }
        else
        {
            // Case1-6: Node - Black
            bool nodeWasLeftChild = node.IsLeftChild;
            
            Transplant(node, child);
            RemoveCase1(node.Parent, child, nodeWasLeftChild);
        }
    }


    // Child - Red
    // protected virtual void OnNodeRemoved(RbNode<TKey, TValue>? p, RbNode<TKey, TValue>? y, bool xWasLeftChild)
    // {
    //     if (IsRed(y))
    //     {
    //         y!.Color = RbColor.Black;
    //     }
    //     else
    //     {
    //         RemoveCase1(p, y, xWasLeftChild);
    //     }
    // }

    // If child is Root/Red => change color on Black
    private void RemoveCase1(RbNode<TKey, TValue>? p, RbNode<TKey, TValue>? y, bool xWasLeftChild)
    {
        if (p == null && y == null) return; // if removed node was Root and has no children
        
        if (y is { Parent: null } || IsRed(y))
        {
            y!.Color = RbColor.Black;
        } else
        {
            RemoveCase2(p, y, xWasLeftChild);
        }
    }

    // Brother - RED
    private void RemoveCase2(RbNode<TKey, TValue>? p, RbNode<TKey, TValue>? y, bool xWasLeftChild)
    {
        RbNode<TKey, TValue>? brother = Brother(p, y, xWasLeftChild);
        if (IsRed(brother))
        {
            p!.Color = RbColor.Red;
            brother!.Color = RbColor.Black;
            
            if (brother.IsLeftChild)
            {
                RotateRight(p);
            }
            else
            {
                RotateLeft(p);
            }
        }
        RemoveCase3(p, y, xWasLeftChild);
    }

    // Brother - Black (or null)
    private void RemoveCase3(RbNode<TKey, TValue>? p, RbNode<TKey, TValue>? y, bool xWasLeftChild)
    {
        RbNode<TKey, TValue>? brother = Brother(p, y, xWasLeftChild);
        if (IsBlack(brother?.Left) && IsBlack(brother?.Right))
        {
            RemoveCase4(p, y, xWasLeftChild);
        }
        else if (brother!.IsLeftChild && IsBlack(brother.Left) || brother.IsRightChild && IsBlack(brother.Right))
        {
            RemoveCase5(p!, y, xWasLeftChild);
        }
        else
        {
            RemoveCase6(p!, y, xWasLeftChild);
        }
    }

    // Brother - Black, both children of brother - black (maybe brother is null)
    private void RemoveCase4(RbNode<TKey, TValue>? p, RbNode<TKey, TValue>? y, bool xWasLeftChild)
    {
        RbNode<TKey, TValue>? brother = Brother(p, y, xWasLeftChild);
        
        brother?.Color = RbColor.Red;
        RemoveCase1(p?.Parent, p, p?.IsLeftChild ?? false);
    }

    // Brother - Black, far-child of brother - black, near-child of brother - red
    private void RemoveCase5(RbNode<TKey, TValue> p, RbNode<TKey, TValue>? y, bool xWasLeftChild)
    {
        RbNode<TKey, TValue> brother = Brother(p, y, xWasLeftChild)!;
        
        brother.Color = RbColor.Red;
        
        if (brother.IsLeftChild)
        {
            brother.Right?.Color = RbColor.Black;
            RotateLeft(brother);
        }
        else
        {
            brother.Left?.Color = RbColor.Black;
            RotateRight(brother);
        }
        RemoveCase6(p, y, xWasLeftChild);
    }
    
    // Brother - Black, far-child of brother - red, near-child of brother - any
    private void RemoveCase6(RbNode<TKey, TValue> p, RbNode<TKey, TValue>? y, bool xWasLeftChild)
    {
        RbNode<TKey, TValue> brother = Brother(p, y, xWasLeftChild)!;

        brother.Color = p.Color;
        
        p.Color = RbColor.Black;
        
        if (brother.IsLeftChild)
        {
            brother.Left!.Color = RbColor.Black; // change color of far-child
            RotateRight(p);
        }
        else
        {
            brother.Right!.Color = RbColor.Black;
            RotateLeft(p);
        }
    }
    
    
}