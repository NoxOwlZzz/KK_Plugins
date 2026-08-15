namespace KK_Plugins.MaterialEditor
{
    internal static class MEAnimationValidation
    {
        internal static bool IsUsable(
            float totalTime,
            int frameCount)
        {
            return frameCount > 0
                   && totalTime > 0f
                   && !float.IsNaN(totalTime)
                   && !float.IsInfinity(totalTime);
        }

        internal static float WrapTime(float time, float totalTime)
        {
            if (totalTime <= 0f
                || float.IsNaN(totalTime)
                || float.IsInfinity(totalTime))
                return 0f;
            if (float.IsNaN(time) || float.IsInfinity(time))
                return 0f;

            const int maxExactSubtractions = 16;
            var subtractions = 0;
            while (time >= totalTime && subtractions < maxExactSubtractions)
            {
                var next = time - totalTime;
                if (next == time)
                    break;
                time = next;
                subtractions++;
            }

            if (time >= totalTime)
                time %= totalTime;
            return time;
        }
    }
}
