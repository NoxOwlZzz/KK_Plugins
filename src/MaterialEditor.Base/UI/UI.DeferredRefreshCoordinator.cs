namespace MaterialEditorAPI
{
    // Owns one mutable deferred request without allocating a request object for
    // every replacement. Coroutine scheduling remains in MaterialEditorUI.
    internal sealed class DeferredRefreshCoordinator
    {
        private int _version;
        private bool _hasPending;
        private object _target;
        private object _data;
        private string _filter;
        private bool _workerRunning;
        private int _countdownVersion;
        private int _framesRemaining;

        internal int Schedule(object target, object data, string filter)
        {
            _version++;
            _target = target;
            _data = data;
            _filter = filter;
            _hasPending = true;
            return _version;
        }

        internal void Cancel()
        {
            _version++;
            _hasPending = false;
            _target = null;
            _data = null;
            _filter = null;
            _workerRunning = false;
            _framesRemaining = 0;
        }

        internal int CurrentVersion
        {
            get { return _version; }
        }

        internal bool IsCurrent(int version)
        {
            return _hasPending && version == _version;
        }

        internal bool TryStartWorker()
        {
            if (_workerRunning || !_hasPending)
                return false;
            _workerRunning = true;
            return true;
        }

        internal void WorkerStopped()
        {
            _workerRunning = false;
        }

        internal bool AdvanceFrame(
            int delayFrames,
            out object target,
            out object data,
            out string filter)
        {
            if (!_hasPending)
            {
                target = null;
                data = null;
                filter = null;
                return false;
            }

            if (_countdownVersion != _version)
            {
                _countdownVersion = _version;
                _framesRemaining = delayFrames < 1 ? 1 : delayFrames;
            }

            _framesRemaining--;
            if (_framesRemaining > 0)
            {
                target = null;
                data = null;
                filter = null;
                return false;
            }

            return TryTake(_countdownVersion, out target, out data, out filter);
        }

        internal bool TryTake(
            int version,
            out object target,
            out object data,
            out string filter)
        {
            if (!IsCurrent(version))
            {
                target = null;
                data = null;
                filter = null;
                return false;
            }

            _hasPending = false;
            target = _target;
            data = _data;
            filter = _filter;
            _target = null;
            _data = null;
            _filter = null;
            _framesRemaining = 0;
            return true;
        }

        internal bool HasPending
        {
            get { return _hasPending; }
        }
    }
}
