using System;
using System.Collections.Generic;
using UnityEngine;

namespace PiggyRace.Netcode.Interpolation
{
    [Serializable]
    public struct NetPigState
    {
        public int Tick;
        public Vector3 Position;
        public float Yaw;
    }

    // Minimal snapshot buffer for interpolation of transforms by tick.
    public class SnapshotBuffer
    {
        private readonly List<NetPigState> _snapshots = new List<NetPigState>(32);

        public void Clear() => _snapshots.Clear();

        public void AddSnapshot(NetPigState s)
        {
            // Insert sorted by Tick (ascending)
            int idx = _snapshots.BinarySearch(s, TickComparer.Instance);
            if (idx < 0) idx = ~idx;
            _snapshots.Insert(idx, s);
            // Keep buffer bounded
            if (_snapshots.Count > 64)
                _snapshots.RemoveAt(0);
        }

        public bool TryInterpolate(int renderTick, out NetPigState result)
        {
            result = default;
            if (_snapshots.Count == 0) return false;
            // Before first snapshot
            if (renderTick <= _snapshots[0].Tick)
            {
                result = _snapshots[0];
                return true;
            }
            // After last snapshot
            var last = _snapshots[_snapshots.Count - 1];
            if (renderTick >= last.Tick)
            {
                result = last;
                return true;
            }

            // Find bracketing states
            int hi = _snapshots.FindIndex(s => s.Tick >= renderTick);
            if (hi <= 0) { result = _snapshots[0]; return true; }
            int lo = hi - 1;
            var a = _snapshots[lo];
            var b = _snapshots[hi];
            float t = Mathf.InverseLerp(a.Tick, b.Tick, renderTick);
            result = new NetPigState
            {
                Tick = renderTick,
                Position = Vector3.LerpUnclamped(a.Position, b.Position, t),
                Yaw = Mathf.LerpAngle(a.Yaw, b.Yaw, t),
            };
            return true;
        }

        private class TickComparer : IComparer<NetPigState>
        {
            public static readonly TickComparer Instance = new TickComparer();
            public int Compare(NetPigState x, NetPigState y) => x.Tick.CompareTo(y.Tick);
        }
    }
}

