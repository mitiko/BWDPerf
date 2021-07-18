using System;
using System.Collections;
using System.Collections.Generic;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Tools
{
    // A sorted linked list with fast binary search
    // TODO: Implement derandomization
    // TODO: Compare performance with p = 2/3
    // TODO: Compare performance with doubly linked skip list
    public class SkipList<T> where T : IComparable<T>
    {
        private readonly SkipListNode<T> _start = new(default, _maxLevel);
        private static readonly int _maxLevel = 33;
        private readonly Random _rnd = new(Seed: 1337);

        public int Count { get; private set; } = 0;

        public SkipList()
        {
            for (int i = 0; i < this._start.Next.Length; i++)
                this._start.Next[i] = null;
        }

        public void Insert(T value)
        {
            var level = 1;
            for (int R = _rnd.Next(); (R & 1) == 1; R >>= 1, level++); // p=1/2
            // for (int R = _rnd.Next(); R % 3 != 0; R /= 3, level++); // p=2/3

            var newNode = new SkipListNode<T>(value, level);
            var curr = this._start;

            // Skip as many nodes as possible in the higher levels until
            // we find the region our node falls into.
            for (int i = _maxLevel - 1; i >= 0 ; i--)
            {
                for (; curr.Next[i] != null; curr = curr.Next[i])
                {
                    if (curr.Next[i].Value.CompareTo(value) > 0) break;
                }

                if (level > i)
                {
                    newNode.Next[i] = curr.Next[i];
                    curr.Next[i] = newNode;
                }
            }

            this.Count++;
        }

        public bool Remove(T value)
        {
            var curr = _start;
            var found = false;

            // Skip as many nodes as possible in the higher levels until
            // we find the region our node falls into.
            for (int i = _maxLevel - 1; i >= 0 ; i--)
            {
                for (; curr.Next[i] != null; curr = curr.Next[i])
                {
                    var comparison = curr.Next[i].Value.CompareTo(value);
                    // If we're on the left of the region, continue
                    // Otherwise we break and drop a level to search within
                    if (comparison < 0) continue;

                    // If we find the element, remove it from the list at the current level
                    // The curr node stays the same, so we should "find" the element again
                    // for the other levels. Furthermore, the branch predictor should pick up 
                    // this pattern pretty easily. 
                    if (comparison == 0)
                    {
                        found = true;
                        curr.Next[i] = curr.Next[i].Next[i];
                    }
                    break;
                }
            }

            if (found) this.Count--;
            return found;
        }

        // Uses binary search to find the first value that satisfies a condition
        // NOTE: For the binary search to work correctly, the predicate must have
        // the following pattern along the sorted data range:
        // FFFFFFFFFFFFFFFFFFFFFFFFFTTTTTTTTTTTT
        // Patterns such as FFFFFFFTTTTTFFFFF will not return the (humanly) expected result
        public SkipListNode<T> BinarySearchFirstInRange(Predicate<T> predicate) => this.BinarySearchFirstInRange(predicate, _start);

        // This start node overload is for optimization purposes, since the parsed location list is already sorted
        public SkipListNode<T> BinarySearchFirstInRange(Predicate<T> predicate, SkipListNode<T> startNode)
        {
            var curr = startNode;

            // Skip as many nodes as possible in the higher levels until
            // we find the region our node falls into.
            for (int i = _maxLevel - 1; i >= 0 ; i--)
            {
                for (; curr.Next[i] != null; curr = curr.Next[i])
                {
                    if (predicate.Invoke(curr.Next[i].Value)) break;
                }
            }

            // If the range was found, curr.Next[0] is the first element that
            // answers the predicate. Otherwise curr.Next[0] is null
            return curr.Next[0];
        }

        public IEnumerable<T> Enumerate()
        {
            for (var node = _start.Next[0]; node != null; node = node.Next[0])
                yield return node.Value;
        }

        public IEnumerable<SkipListNode<T>> EnumerateNodes()
        {
            for (var node = _start.Next[0]; node != null; node = node.Next[0])
                yield return node;
        }

        public class SkipListNode<J>
        {
            public J Value { get; set; }
            public SkipListNode<J>[] Next { get; }

            public int Level => this.Next.Length;

            public SkipListNode(J value, int level)
            {
                this.Value = value;
                this.Next = new SkipListNode<J>[level];
            }
        }
    }    
}