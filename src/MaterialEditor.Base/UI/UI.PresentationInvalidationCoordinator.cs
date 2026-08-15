using System;
using System.Collections.Generic;

namespace MaterialEditorAPI
{
    [Flags]
    internal enum PresentationInvalidationReason
    {
        None = 0,
        Search = 1,
        Conditions = 2
    }

    internal struct PresentationInvalidationWorkerLease
    {
        private readonly int _generation;
        private readonly long _leaseId;

        internal PresentationInvalidationWorkerLease(int generation, long leaseId)
        {
            _generation = generation;
            _leaseId = leaseId;
        }

        internal int Generation
        {
            get { return _generation; }
        }

        internal long LeaseId
        {
            get { return _leaseId; }
        }
    }

    internal sealed class PresentationInvalidationBatch<TConditionSource>
    {
        private readonly int _generation;
        private readonly long _version;
        private readonly PresentationInvalidationReason _reason;
        private readonly TConditionSource[] _conditionSources;

        internal PresentationInvalidationBatch(
            int generation,
            long version,
            PresentationInvalidationReason reason,
            TConditionSource[] conditionSources)
        {
            _generation = generation;
            _version = version;
            _reason = reason;
            _conditionSources = conditionSources;
        }

        internal int Generation
        {
            get { return _generation; }
        }

        internal long Version
        {
            get { return _version; }
        }

        internal PresentationInvalidationReason Reason
        {
            get { return _reason; }
        }

        internal TConditionSource[] ConditionSources
        {
            get { return _conditionSources; }
        }
    }

    // Pure coordinator for a Unity owner to drive from its end-of-frame worker.
    // It deliberately has no locking: construct and use it only on the main thread.
    internal sealed class PresentationInvalidationCoordinator<TConditionSource>
    {
        private static readonly TConditionSource[] NoConditionSources =
            new TConditionSource[0];

        private readonly HashSet<TConditionSource> _pendingConditionSet;
        private readonly List<TConditionSource> _pendingConditionSources =
            new List<TConditionSource>();

        private int _generation = 1;
        private long _version;
        private long _pendingVersion;
        private long _nextLeaseId;
        private long _activeLeaseId;
        private bool _searchPending;
        private bool _workerActive;
        private bool _flushActive;
        private bool _hasLastFlushFrame;
        private int _lastFlushFrameId;

        internal PresentationInvalidationCoordinator()
            : this(null)
        {
        }

        internal PresentationInvalidationCoordinator(
            IEqualityComparer<TConditionSource> conditionSourceComparer)
        {
            _pendingConditionSet = conditionSourceComparer == null
                ? new HashSet<TConditionSource>()
                : new HashSet<TConditionSource>(conditionSourceComparer);
        }

        internal int Generation
        {
            get { return _generation; }
        }

        internal long Version
        {
            get { return _version; }
        }

        internal bool HasPending
        {
            get { return _searchPending || _pendingConditionSources.Count != 0; }
        }

        internal bool HasActiveWorker
        {
            get { return _workerActive; }
        }

        internal bool IsFlushActive
        {
            get { return _flushActive; }
        }

        internal int PendingConditionCount
        {
            get { return _pendingConditionSources.Count; }
        }

        internal bool RequestSearch()
        {
            AdvanceVersion();
            _pendingVersion = _version;

            var changed = !_searchPending || _pendingConditionSources.Count != 0;
            _searchPending = true;
            _pendingConditionSources.Clear();
            _pendingConditionSet.Clear();
            return changed;
        }

        internal bool RequestCondition(TConditionSource source)
        {
            AdvanceVersion();
            _pendingVersion = _version;

            // A search rebuild observes all condition state, so retaining individual
            // condition sources in the same batch would only duplicate work.
            if (_searchPending || !_pendingConditionSet.Add(source))
                return false;

            _pendingConditionSources.Add(source);
            return true;
        }

        internal bool TryAcquireWorker(
            out PresentationInvalidationWorkerLease lease)
        {
            if (_workerActive || !HasPending)
            {
                lease = new PresentationInvalidationWorkerLease();
                return false;
            }

            _workerActive = true;
            _activeLeaseId = NextLeaseId();
            lease = new PresentationInvalidationWorkerLease(
                _generation,
                _activeLeaseId);
            return true;
        }

        internal bool IsWorkerLeaseCurrent(
            PresentationInvalidationWorkerLease lease)
        {
            return _workerActive &&
                   lease.Generation == _generation &&
                   lease.LeaseId != 0 &&
                   lease.LeaseId == _activeLeaseId;
        }

        internal bool IsGenerationCurrent(int generation)
        {
            return generation == _generation;
        }

        internal bool TryBeginFlush(
            PresentationInvalidationWorkerLease lease,
            int frameId,
            out PresentationInvalidationBatch<TConditionSource> batch)
        {
            batch = null;
            if (!IsWorkerLeaseCurrent(lease) ||
                _flushActive ||
                !HasPending ||
                (_hasLastFlushFrame && frameId == _lastFlushFrameId))
                return false;

            var reason = _searchPending
                ? PresentationInvalidationReason.Search
                : PresentationInvalidationReason.Conditions;
            var sources = _searchPending
                ? NoConditionSources
                : _pendingConditionSources.ToArray();

            batch = new PresentationInvalidationBatch<TConditionSource>(
                _generation,
                _pendingVersion,
                reason,
                sources);

            // Clear the pending batch before returning it. Requests made by the
            // flush callback therefore form a distinct batch for a later frame.
            _searchPending = false;
            _pendingConditionSources.Clear();
            _pendingConditionSet.Clear();
            _pendingVersion = 0;
            _flushActive = true;
            _hasLastFlushFrame = true;
            _lastFlushFrameId = frameId;
            return true;
        }

        // Exceptional synchronous recovery path for an owner whose coroutine
        // could not be started. It drains the already-acquired lease without an
        // end-of-frame gate so pending work cannot be left without a worker.
        internal bool TryBeginRecoveryFlush(
            PresentationInvalidationWorkerLease lease,
            out PresentationInvalidationBatch<TConditionSource> batch)
        {
            batch = null;
            if (!IsWorkerLeaseCurrent(lease)
                || _flushActive
                || !HasPending)
                return false;

            var reason = _searchPending
                ? PresentationInvalidationReason.Search
                : PresentationInvalidationReason.Conditions;
            var sources = _searchPending
                ? NoConditionSources
                : _pendingConditionSources.ToArray();
            batch = new PresentationInvalidationBatch<TConditionSource>(
                _generation,
                _pendingVersion,
                reason,
                sources);
            _searchPending = false;
            _pendingConditionSources.Clear();
            _pendingConditionSet.Clear();
            _pendingVersion = 0;
            _flushActive = true;
            return true;
        }

        // Returns true when the same worker lease must wait for another frame.
        internal bool CompleteFlush(
            PresentationInvalidationWorkerLease lease)
        {
            if (!IsWorkerLeaseCurrent(lease) || !_flushActive)
                return false;

            _flushActive = false;
            if (HasPending)
                return true;

            ReleaseWorkerLease();
            return false;
        }

        // Used when the owner could not start its coroutine. Pending work is kept
        // so a later request can acquire a fresh worker lease and retry it.
        internal bool AbandonWorker(
            PresentationInvalidationWorkerLease lease)
        {
            if (!IsWorkerLeaseCurrent(lease) || _flushActive)
                return false;

            ReleaseWorkerLease();
            return true;
        }

        internal void Cancel()
        {
            AdvanceGeneration();
            AdvanceVersion();
            _pendingVersion = 0;
            _searchPending = false;
            _pendingConditionSources.Clear();
            _pendingConditionSet.Clear();
            _flushActive = false;
            ReleaseWorkerLease();

            // Do not reset the last frame marker. A cancelled owner and its
            // replacement must still be unable to flush twice in one frame.
        }

        private void ReleaseWorkerLease()
        {
            _workerActive = false;
            _activeLeaseId = 0;
        }

        private long NextLeaseId()
        {
            unchecked
            {
                _nextLeaseId++;
                if (_nextLeaseId == 0)
                    _nextLeaseId++;
            }
            return _nextLeaseId;
        }

        private void AdvanceGeneration()
        {
            unchecked
            {
                _generation++;
                if (_generation == 0)
                    _generation++;
            }
        }

        private void AdvanceVersion()
        {
            unchecked
            {
                _version++;
                if (_version == 0)
                    _version++;
            }
        }
    }
}
