using MaterialEditorAPI;

internal static class CubemapBackgroundReadTests
{
    internal static void Run()
    {
        FileAndHashWorkCompleteOnAWorker();
        MissingFilesReportAUsefulError();
        DisposalDiscardsCompletedData();
    }

    private static void FileAndHashWorkCompleteOnAWorker()
    {
        var path = Path.GetTempFileName();
        try
        {
            var expected = new byte[] { 137, 80, 78, 71, 1, 2, 3, 4 };
            File.WriteAllBytes(path, expected);
            var callerThread = Environment.CurrentManagedThreadId;
            using (var operation = MaterialEditorCubemapBackgroundRead.Begin(path))
            {
                Wait(operation, "background Cubemap read");
                byte[] actual;
                MaterialEditorCubemapContentKey key;
                string error;
                True(
                    operation.TryTakeResult(out actual, out key, out error),
                    "completed result available");
                Equal(null, error, "background read error");
                True(actual.SequenceEqual(expected), "background read bytes");
                True(key != null && key.Matches(actual), "hash is bound to returned bytes");
                True(
                    operation.WorkerThreadId != 0
                    && operation.WorkerThreadId != callerThread,
                    "file and hash work ran outside the caller thread");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void MissingFilesReportAUsefulError()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "material-editor-missing-cubemap-" + Guid.NewGuid() + ".png");
        using (var operation = MaterialEditorCubemapBackgroundRead.Begin(path))
        {
            Wait(operation, "missing Cubemap read");
            byte[] data;
            MaterialEditorCubemapContentKey key;
            string error;
            True(
                operation.TryTakeResult(out data, out key, out error),
                "missing result available");
            Equal(null, data, "missing data");
            Equal(null, key, "missing key");
            True(
                error != null && error.Contains("no longer exists"),
                "missing file reason");
        }
    }

    private static void DisposalDiscardsCompletedData()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            var operation = MaterialEditorCubemapBackgroundRead.Begin(path);
            Wait(operation, "disposable Cubemap read");
            operation.Dispose();

            byte[] data;
            MaterialEditorCubemapContentKey key;
            string error;
            True(
                operation.TryTakeResult(out data, out key, out error),
                "cancelled result available");
            Equal(null, data, "disposed data released");
            Equal(null, key, "disposed key released");
            True(
                error != null && error.Contains("cancelled"),
                "disposed read reports cancellation");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void Wait(
        MaterialEditorCubemapBackgroundRead operation,
        string name)
    {
        if (!SpinWait.SpinUntil(() => operation.IsComplete, 5000))
            throw new InvalidOperationException(name + " timed out.");
    }

    private static void True(bool value, string name)
    {
        if (!value)
            throw new InvalidOperationException(name);
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                name + ": expected " + expected + ", got " + actual);
    }
}
