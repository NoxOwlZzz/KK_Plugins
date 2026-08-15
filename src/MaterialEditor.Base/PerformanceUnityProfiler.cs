using UnityEngine.Profiling;

namespace MaterialEditorAPI
{
    // Optional adapter. The pure instrumentation core has no Unity dependency.
    internal sealed class MaterialEditorUnityProfiler :
        IMaterialEditorPerformanceProfiler
    {
        internal static readonly MaterialEditorUnityProfiler Instance =
            new MaterialEditorUnityProfiler();

        private MaterialEditorUnityProfiler()
        {
        }

        public void BeginSample(MaterialEditorPerformanceMetric metric)
        {
            Profiler.BeginSample(MaterialEditorPerformance.GetMetricName(metric));
        }

        public void EndSample()
        {
            Profiler.EndSample();
        }
    }
}
