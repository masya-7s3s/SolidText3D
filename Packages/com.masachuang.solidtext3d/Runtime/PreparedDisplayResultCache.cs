using System;
using System.Collections.Generic;

namespace MasaChuang.SolidText3D
{
    internal sealed class PreparedResultCacheEntry
    {
        public DisplayResultSignature Signature { get; set; }

        public PreparedDisplayResult Result { get; set; }

        public long LastUsedTick { get; set; }
    }

    internal sealed class PreparedDisplayResultCache
    {
        private readonly Dictionary<DisplayResultSignature, PreparedResultCacheEntry> _entries;
        private readonly int _capacity;
        private long _tick;

        public PreparedDisplayResultCache(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _capacity = capacity;
            _entries = new Dictionary<DisplayResultSignature, PreparedResultCacheEntry>(capacity);
        }

        public int Capacity => _capacity;

        public int Count => _entries.Count;

        public bool TryGet(DisplayResultSignature signature, out PreparedDisplayResult result)
        {
            if (_entries.TryGetValue(signature, out PreparedResultCacheEntry entry))
            {
                entry.LastUsedTick = NextTick();
                result = entry.Result;
                return true;
            }

            result = null;
            return false;
        }

        public void Store(PreparedDisplayResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            DisplayResultSignature signature = result.Signature;
            if (_entries.TryGetValue(signature, out PreparedResultCacheEntry existing))
            {
                existing.Result = result;
                existing.LastUsedTick = NextTick();
                return;
            }

            if (_entries.Count >= _capacity)
                EvictLeastRecentlyUsed();

            _entries[signature] = new PreparedResultCacheEntry
            {
                Signature = signature,
                Result = result,
                LastUsedTick = NextTick()
            };
        }

        public void Clear()
        {
            _entries.Clear();
            _tick = 0;
        }

        private void EvictLeastRecentlyUsed()
        {
            DisplayResultSignature victimKey = default;
            long oldestTick = long.MaxValue;
            bool found = false;

            foreach (KeyValuePair<DisplayResultSignature, PreparedResultCacheEntry> pair in _entries)
            {
                if (pair.Value.LastUsedTick >= oldestTick)
                    continue;

                victimKey = pair.Key;
                oldestTick = pair.Value.LastUsedTick;
                found = true;
            }

            if (found)
                _entries.Remove(victimKey);
        }

        private long NextTick()
        {
            _tick++;
            return _tick;
        }
    }
}