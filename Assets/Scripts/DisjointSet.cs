using System;

namespace DisjointSetMaze
{
    public sealed class DisjointSet
    {
        private readonly int[] _parents;
        private readonly int[] _ranks;

        public int ComponentCount { get; private set; }

        public DisjointSet(int size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));

            _parents = new int[size];
            _ranks = new int[size];
            ComponentCount = size;

            for (var index = 0; index < size; index++)
            {
                _parents[index] = index;
            }
        }

        public int FindSet(int element)
        {
            ValidateElement(element);

            var root = element;
            while (_parents[root] != root)
            {
                root = _parents[root];
            }

            while (_parents[element] != element)
            {
                var parent = _parents[element];
                _parents[element] = root;
                element = parent;
            }

            return root;
        }

        public bool UnionSet(int first, int second)
        {
            var firstRoot = FindSet(first);
            var secondRoot = FindSet(second);

            if (firstRoot == secondRoot) return false;

            if (_ranks[firstRoot] < _ranks[secondRoot])
            {
                var temporaryRoot = firstRoot;
                firstRoot = secondRoot;
                secondRoot = temporaryRoot;
            }

            _parents[secondRoot] = firstRoot;
            if (_ranks[firstRoot] == _ranks[secondRoot])
            {
                _ranks[firstRoot]++;
            }

            ComponentCount--;
            return true;
        }

        private void ValidateElement(int element)
        {
            if (element < 0 || element >= _parents.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(element));
            }
        }
    }
}
