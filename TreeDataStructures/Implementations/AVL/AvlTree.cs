using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        RebalanceFrom(newNode.Parent ?? newNode);
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        RebalanceFrom(parent ?? child);
    }

    private static int H(AvlNode<TKey, TValue>? node) => node?.Height ?? 0;
    
    private static int Bf(AvlNode<TKey, TValue> node) => H(node.Right) - H(node.Left);
    

    private static void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        node.Height = 1 + Math.Max(H(node.Right), H(node.Left));
    }

    private void RebalanceFrom(AvlNode<TKey, TValue>? node)
    {
        while (node != null)
        {
            UpdateHeight(node);
            int bf = Bf(node);

            AvlNode<TKey, TValue> subtreeRoot = node;

            if (bf < -1) // left heavy
            {
                if (Bf(node.Left!) > 0) // LR
                {
                    RotateLeft(node.Left!);
                    UpdateHeight(node.Left!); // бывший левый после поворота
                }

                RotateRight(node); // LL/LR

                // после RotateRight(node) сам node стал правым ребенком нового корня
                UpdateHeight(node);
                UpdateHeight(node.Parent!);
                subtreeRoot = node.Parent!;
            }
            else if (bf > 1) // right heavy
            {
                if (Bf(node.Right!) < 0) // RL
                {
                    RotateRight(node.Right!);
                    UpdateHeight(node.Right!); // бывший правый после поворота
                }

                RotateLeft(node); // RR/RL

                // после RotateLeft(node) сам node стал левым ребенком нового корня
                UpdateHeight(node);
                UpdateHeight(node.Parent!);
                subtreeRoot = node.Parent!;
            }

            // гарантированно идем выше обработанного поддерева
            node = subtreeRoot.Parent;
        }
    }
}