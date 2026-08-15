# Material Editor Performance Harness

This `net8.0` executable runs without Koikatsu or Unity. It links the pure
production metadata parser, condition policy, and performance instrumentation,
then exercises deterministic synthetic workloads.

Default gate:

```powershell
dotnet run --project tests\MaterialEditor.PerformanceHarness\MaterialEditor.PerformanceHarness.csproj -c Release --no-restore
```

Optional outputs:

```text
--json <path>              Write the current report.
--save-baseline <path>     Save the current-strategy baseline JSON.
--compare-baseline <path>  Compare semantic fingerprints and print metric deltas.
```

The gate validates semantic fingerprints, exact operation counts, listener and
provider lifetime, and bounded pooling. Median/P95 timing and allocation values
are evidence only; absolute time thresholds are deliberately not pass/fail gates.
