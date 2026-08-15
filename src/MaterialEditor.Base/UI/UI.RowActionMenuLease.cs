using System;

namespace MaterialEditorAPI
{
    // Pure ownership lease shared by the Unity menu and the standalone tests.
    // Taking an action clears every retained callback before caller code runs.
    internal sealed class RowActionMenuLease
    {
        internal const int MaximumActions = 4;

        private readonly Action[] _actions = new Action[MaximumActions];
        private object _owner;
        private int _generation;
        private int _count;

        internal object Owner => _owner;
        internal int Generation => _generation;
        internal int Count => _count;
        internal bool IsOpen => _owner != null;

        internal void Begin(object owner, int generation)
        {
            Close();
            if (owner == null)
                return;

            _owner = owner;
            _generation = generation;
        }

        internal int Add(Action action)
        {
            if (_owner == null || action == null || _count >= MaximumActions)
                return -1;

            var index = _count;
            _actions[index] = action;
            _count++;
            return index;
        }

        internal Action Take(
            int index,
            object expectedOwner,
            int expectedGeneration,
            bool ownerActive,
            bool actionEnabled)
        {
            if (!ReferenceEquals(_owner, expectedOwner)
                || _generation != expectedGeneration
                || !ownerActive
                || !actionEnabled
                || index < 0
                || index >= _count)
            {
                Close();
                return null;
            }

            var action = _actions[index];
            Close();
            return action;
        }

        internal void CloseIfOwner(object owner)
        {
            if (ReferenceEquals(_owner, owner))
                Close();
        }

        internal void Close()
        {
            _owner = null;
            _generation = 0;
            _count = 0;
            for (var index = 0; index < _actions.Length; index++)
                _actions[index] = null;
        }
    }
}
