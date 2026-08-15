namespace KK_Plugins.MaterialEditor
{
    internal struct EndOfFrameRefreshGate
    {
        private bool _pending;

        internal bool IsPending => _pending;

        internal bool TryRequest()
        {
            if (_pending)
                return false;
            _pending = true;
            return true;
        }

        internal void Complete()
        {
            _pending = false;
        }
    }
}
