using MaterialEditorAPI;

internal static class CubemapMemoryBudgetTests
{
    internal static void Run()
    {
        MaximumSupportedImportFitsBudget();
        MaximumSupportedHdrImportFitsBudget();
        MaximumSupportedExportFitsBudget();
        OversizedExportIsRejectedByBudget();
        InvalidDimensionsSaturateSafely();
        ConcurrentReservationsRespectAggregateBudget();
    }

    private static void MaximumSupportedHdrImportFitsBudget()
    {
        var pngEstimate = MaterialEditorCubemapMemoryBudget.EstimateImportPeakBytes(
            MaterialEditorCubemapProjection.MaximumSourceFileBytes,
            4096,
            2048,
            1024,
            false);
        long estimate;
        string error;
        True(
            MaterialEditorCubemapMemoryBudget.TryValidateImport(
                MaterialEditorCubemapProjection.MaximumSourceFileBytes,
                4096,
                2048,
                1024,
                true,
                out estimate,
                out error),
            "maximum supported Radiance HDR import budget");
        True(estimate > 200L * 1024L * 1024L,
            "HDR import estimate includes RGBAHalf ownership");
        True(estimate < pngEstimate,
            "packed RGBE avoids PNG decoder and Texture2D source copies");
        True(estimate <= MaterialEditorCubemapMemoryBudget.DefaultPeakBytes,
            "HDR import estimate within budget");
        Equal(null, error, "maximum HDR import budget error");
    }

    private static void MaximumSupportedImportFitsBudget()
    {
        long estimate;
        string error;
        True(
            MaterialEditorCubemapMemoryBudget.TryValidateImport(
                MaterialEditorCubemapProjection.MaximumSourceFileBytes,
                4096,
                2048,
                1024,
                out estimate,
                out error),
            "maximum supported import budget");
        True(estimate > 250L * 1024L * 1024L, "import estimate is conservative");
        True(estimate <= MaterialEditorCubemapMemoryBudget.DefaultPeakBytes,
            "import estimate within budget");
        Equal(null, error, "maximum import budget error");
    }

    private static void MaximumSupportedExportFitsBudget()
    {
        long estimate;
        string error;
        True(
            MaterialEditorCubemapMemoryBudget.TryValidateExport(
                1024,
                out estimate,
                out error),
            "maximum supported export budget");
        True(estimate > 250L * 1024L * 1024L, "export estimate is conservative");
        True(estimate <= MaterialEditorCubemapMemoryBudget.DefaultPeakBytes,
            "export estimate within budget");
        Equal(null, error, "maximum export budget error");
    }

    private static void OversizedExportIsRejectedByBudget()
    {
        long estimate;
        string error;
        True(
            !MaterialEditorCubemapMemoryBudget.TryValidateExport(
                2048,
                out estimate,
                out error),
            "oversized export budget rejected");
        True(estimate > MaterialEditorCubemapMemoryBudget.DefaultPeakBytes,
            "oversized export estimate above budget");
        True(error != null && error.Contains("conversion budget"),
            "oversized export budget reason");
    }

    private static void InvalidDimensionsSaturateSafely()
    {
        Equal(
            long.MaxValue,
            MaterialEditorCubemapMemoryBudget.EstimateImportPeakBytes(
                1,
                int.MaxValue,
                int.MaxValue,
                int.MaxValue),
            "overflowing import estimate saturates");
        Equal(
            long.MaxValue,
            MaterialEditorCubemapMemoryBudget.EstimateExportPeakBytes(
                int.MaxValue,
                true),
            "overflowing export estimate saturates");
    }

    private static void ConcurrentReservationsRespectAggregateBudget()
    {
        var admission = new MaterialEditorCubemapMemoryAdmission(100L);
        MaterialEditorCubemapMemoryReservation first;
        MaterialEditorCubemapMemoryReservation second;
        string error;
        True(admission.TryReserve(60L, out first, out error),
            "first aggregate reservation");
        Equal(60L, admission.ReservedBytes, "first aggregate byte count");
        True(!admission.TryReserve(50L, out second, out error),
            "aggregate overflow rejected");
        True(error != null && error.Contains("already using"),
            "aggregate overflow reason");
        Equal(null, second, "rejected aggregate reservation");

        first.Dispose();
        first.Dispose();
        Equal(0L, admission.ReservedBytes, "reservation disposal is idempotent");
        True(admission.TryReserve(100L, out second, out error),
            "budget reusable after release");
        second.Dispose();
        Equal(0L, admission.ReservedBytes, "aggregate budget fully released");
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
