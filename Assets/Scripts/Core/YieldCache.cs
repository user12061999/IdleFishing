using System.Collections.Generic;
using UnityEngine;

namespace IdleFishing.Core
{
    /// <summary>
    /// High-performance cache for coroutine yield instructions to eliminate heap garbage collection allocations.
    /// </summary>
    public static class YieldCache
    {
        private class FloatComparer : IEqualityComparer<float>
        {
            public bool Equals(float x, float y) => Mathf.Abs(x - y) < 0.0001f;
            public int GetHashCode(float obj) => Mathf.RoundToInt(obj * 1000f).GetHashCode();
        }

        private static readonly Dictionary<float, WaitForSeconds> timeIntervals = new Dictionary<float, WaitForSeconds>(new FloatComparer());
        public static readonly WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();
        public static readonly WaitForFixedUpdate WaitForFixedUpdate = new WaitForFixedUpdate();

        /// <summary>
        /// Returns a cached WaitForSeconds instance rounded to 2 decimal places to prevent GC allocation.
        /// </summary>
        public static WaitForSeconds WaitForSeconds(float seconds)
        {
            if (seconds <= 0f) seconds = 0.01f;
            float rounded = Mathf.Round(seconds * 100f) * 0.01f;
            if (!timeIntervals.TryGetValue(rounded, out var wfs))
            {
                wfs = new WaitForSeconds(rounded);
                timeIntervals[rounded] = wfs;
            }
            return wfs;
        }
    }
}
