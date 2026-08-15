using MaterialEditorAPI;

internal static class PresentationInvalidationCoordinatorTests
{
    internal static void Run()
    {
        FiveHundredRequestsUseOneWorkerLease();
        ConditionSourcesAreDeduplicatedInRequestOrder();
        ReentrantRequestsWaitForTheFollowingFrame();
        CancellationSeparatesTargetAFromTargetB();
        StaleGenerationsCannotFlushOrComplete();
        OneWorkerCanFlushAcrossOneHundredFrames();
        SearchDominatesConditionSourcesInTheSameBatch();
        Console.WriteLine("Presentation invalidation coordinator tests passed.");
    }

    private static void FiveHundredRequestsUseOneWorkerLease()
    {
        var coordinator = CreateCoordinator();
        var workerStarts = 0;
        var lease = new PresentationInvalidationWorkerLease();

        for (var request = 0; request < 500; request++)
        {
            coordinator.RequestCondition("source-" + request);
            PresentationInvalidationWorkerLease candidate;
            if (coordinator.TryAcquireWorker(out candidate))
            {
                lease = candidate;
                workerStarts++;
            }
        }

        Equal(1, workerStarts, "500 requests acquire one worker lease");
        Equal(500, coordinator.PendingConditionCount,
            "500 unique condition sources accumulate");

        PresentationInvalidationBatch<string> batch;
        Equal(true, coordinator.TryBeginFlush(lease, 10, out batch),
            "accumulated batch begins");
        Equal(PresentationInvalidationReason.Conditions, batch.Reason,
            "condition batch reason");
        Equal(500, batch.ConditionSources.Length,
            "all unique condition sources flush together");
        Equal(500L, batch.Version, "batch captures the latest request version");
        Equal(false, coordinator.CompleteFlush(lease),
            "idle worker lease ends after flush");
        Equal(false, coordinator.HasActiveWorker,
            "completed idle worker is released");
    }

    private static void ConditionSourcesAreDeduplicatedInRequestOrder()
    {
        var coordinator = CreateCoordinator();
        Equal(true, coordinator.RequestCondition("A"), "first A changes batch");
        Equal(false, coordinator.RequestCondition("A"), "duplicate A is deduplicated");
        Equal(true, coordinator.RequestCondition("B"), "first B changes batch");
        Equal(false, coordinator.RequestCondition("A"), "later A stays deduplicated");
        Equal(2, coordinator.PendingConditionCount, "two unique sources pending");

        PresentationInvalidationWorkerLease lease;
        Equal(true, coordinator.TryAcquireWorker(out lease), "dedupe worker starts");
        PresentationInvalidationBatch<string> batch;
        Equal(true, coordinator.TryBeginFlush(lease, 20, out batch),
            "dedupe batch begins");
        Equal(2, batch.ConditionSources.Length, "dedupe batch source count");
        Equal("A", batch.ConditionSources[0], "first occurrence order A");
        Equal("B", batch.ConditionSources[1], "first occurrence order B");
        Equal(4L, batch.Version, "deduplicated requests still advance version");
        coordinator.CompleteFlush(lease);
    }

    private static void ReentrantRequestsWaitForTheFollowingFrame()
    {
        var coordinator = CreateCoordinator();
        coordinator.RequestCondition("current");
        PresentationInvalidationWorkerLease lease;
        Equal(true, coordinator.TryAcquireWorker(out lease),
            "reentrant test worker starts");

        PresentationInvalidationBatch<string> first;
        Equal(true, coordinator.TryBeginFlush(lease, 30, out first),
            "first batch begins");
        Equal(true, coordinator.IsFlushActive, "flush marked active");

        // This models a UI callback requesting more work while the first batch
        // is being applied. It must remain on the same lease but a new batch.
        coordinator.RequestCondition("reentrant");
        PresentationInvalidationWorkerLease secondLease;
        Equal(false, coordinator.TryAcquireWorker(out secondLease),
            "reentrant request cannot acquire a second worker");
        Equal(true, coordinator.CompleteFlush(lease),
            "same worker remains leased for reentrant work");

        PresentationInvalidationBatch<string> blocked;
        Equal(false, coordinator.TryBeginFlush(lease, 30, out blocked),
            "reentrant work cannot flush in the same frame");
        Equal(null, blocked, "same-frame begin returns no batch");

        PresentationInvalidationBatch<string> next;
        Equal(true, coordinator.TryBeginFlush(lease, 31, out next),
            "reentrant work begins in the following frame");
        Equal(1, next.ConditionSources.Length, "next batch source count");
        Equal("reentrant", next.ConditionSources[0], "next batch source");
        Equal(false, coordinator.CompleteFlush(lease),
            "worker releases after reentrant batch");
    }

    private static void CancellationSeparatesTargetAFromTargetB()
    {
        var coordinator = CreateCoordinator();
        coordinator.RequestCondition("target-A");
        PresentationInvalidationWorkerLease targetALease;
        Equal(true, coordinator.TryAcquireWorker(out targetALease),
            "target A worker starts");

        var targetAGeneration = coordinator.Generation;
        coordinator.Cancel();
        coordinator.RequestCondition("target-B");
        PresentationInvalidationWorkerLease targetBLease;
        Equal(true, coordinator.TryAcquireWorker(out targetBLease),
            "target B gets a fresh worker");
        Equal(false, targetALease.LeaseId == targetBLease.LeaseId,
            "target B lease identity differs");
        Equal(false, targetAGeneration == targetBLease.Generation,
            "target B generation differs");

        PresentationInvalidationBatch<string> staleBatch;
        Equal(false, coordinator.TryBeginFlush(targetALease, 40, out staleBatch),
            "cancelled target A cannot flush target B");
        Equal(false, coordinator.CompleteFlush(targetALease),
            "cancelled target A cannot complete target B worker");
        Equal(true, coordinator.IsWorkerLeaseCurrent(targetBLease),
            "stale target A operations leave target B lease intact");

        PresentationInvalidationBatch<string> targetBBatch;
        Equal(true, coordinator.TryBeginFlush(targetBLease, 40, out targetBBatch),
            "target B flushes normally");
        Equal(1, targetBBatch.ConditionSources.Length, "target B source count");
        Equal("target-B", targetBBatch.ConditionSources[0],
            "target A source was discarded");
        coordinator.CompleteFlush(targetBLease);
    }

    private static void StaleGenerationsCannotFlushOrComplete()
    {
        var coordinator = CreateCoordinator();
        coordinator.RequestSearch();
        PresentationInvalidationWorkerLease lease;
        coordinator.TryAcquireWorker(out lease);
        PresentationInvalidationBatch<string> batch;
        Equal(true, coordinator.TryBeginFlush(lease, 50, out batch),
            "stale-generation batch begins before cancellation");

        coordinator.Cancel();
        Equal(false, coordinator.IsGenerationCurrent(batch.Generation),
            "cancel invalidates an already-issued batch generation");
        Equal(false, coordinator.IsWorkerLeaseCurrent(lease),
            "cancel invalidates the old worker lease");
        Equal(false, coordinator.CompleteFlush(lease),
            "stale completion is a no-op");
        Equal(false, coordinator.AbandonWorker(lease),
            "stale abandon is a no-op");
        Equal(false, coordinator.HasPending,
            "cancelled generation retains no pending work");
    }

    private static void OneWorkerCanFlushAcrossOneHundredFrames()
    {
        var coordinator = CreateCoordinator();
        coordinator.RequestCondition("frame-0");
        PresentationInvalidationWorkerLease lease;
        Equal(true, coordinator.TryAcquireWorker(out lease),
            "100-frame worker starts once");

        for (var frame = 0; frame < 100; frame++)
        {
            PresentationInvalidationBatch<string> batch;
            Equal(true, coordinator.TryBeginFlush(lease, 1000 + frame, out batch),
                "frame " + frame + " begins exactly one batch");
            Equal("frame-" + frame, batch.ConditionSources[0],
                "frame " + frame + " has its own source");

            if (frame < 99)
                coordinator.RequestCondition("frame-" + (frame + 1));

            var waitsAgain = coordinator.CompleteFlush(lease);
            Equal(frame < 99, waitsAgain,
                "frame " + frame + " worker continuation state");

            if (frame < 99)
            {
                PresentationInvalidationBatch<string> sameFrame;
                Equal(false,
                    coordinator.TryBeginFlush(lease, 1000 + frame, out sameFrame),
                    "frame " + frame + " cannot flush twice");
            }
        }

        Equal(false, coordinator.HasActiveWorker,
            "single worker releases after 100 frames");
    }

    private static void SearchDominatesConditionSourcesInTheSameBatch()
    {
        var coordinator = CreateCoordinator();
        coordinator.RequestCondition("before-search-A");
        coordinator.RequestCondition("before-search-B");
        Equal(true, coordinator.RequestSearch(),
            "search supersedes accumulated conditions");
        Equal(false, coordinator.RequestCondition("after-search"),
            "condition request is absorbed by pending search");
        Equal(0, coordinator.PendingConditionCount,
            "search retains no redundant condition sources");

        PresentationInvalidationWorkerLease lease;
        Equal(true, coordinator.TryAcquireWorker(out lease),
            "search worker starts");
        PresentationInvalidationBatch<string> batch;
        Equal(true, coordinator.TryBeginFlush(lease, 60, out batch),
            "search batch begins");
        Equal(PresentationInvalidationReason.Search, batch.Reason,
            "search is the dominant reason");
        Equal(0, batch.ConditionSources.Length,
            "dominant search batch contains no condition sources");
        Equal(4L, batch.Version,
            "absorbed condition request still advances batch version");
        coordinator.CompleteFlush(lease);
    }

    private static PresentationInvalidationCoordinator<string> CreateCoordinator()
    {
        return new PresentationInvalidationCoordinator<string>(StringComparer.Ordinal);
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', got '" + actual + "'.");
    }
}
