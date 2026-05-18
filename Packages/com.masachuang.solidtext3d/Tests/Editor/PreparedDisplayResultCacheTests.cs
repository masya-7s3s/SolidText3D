using NUnit.Framework;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    public class PreparedDisplayResultCacheTests
    {
        [Test]
        public void DisplayResultSignature_SameValues_AreEqual()
        {
            var left = new DisplayResultSignature(10, 20, ObjectMode.SingleObject);
            var right = new DisplayResultSignature(10, 20, ObjectMode.SingleObject);

            Assert.AreEqual(left, right);
            Assert.IsTrue(left == right);
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Test]
        public void Store_ThenTryGet_ReturnsStoredResult()
        {
            var cache = new PreparedDisplayResultCache(2);
            var expected = CreateResult(1, 100);

            cache.Store(expected);

            bool found = cache.TryGet(expected.Signature, out PreparedDisplayResult actual);

            Assert.IsTrue(found);
            Assert.AreSame(expected, actual);
            Assert.AreEqual(1, cache.Count);
        }

        [Test]
        public void TryGet_WhenMissing_ReturnsFalseAndNull()
        {
            var cache = new PreparedDisplayResultCache(2);

            bool found = cache.TryGet(new DisplayResultSignature(999, 1999, ObjectMode.SingleObject), out PreparedDisplayResult actual);

            Assert.IsFalse(found);
            Assert.IsNull(actual);
        }

        [Test]
        public void Store_WhenCapacityExceeded_EvictsLeastRecentlyUsed()
        {
            var cache = new PreparedDisplayResultCache(2);
            var first = CreateResult(1, 101);
            var second = CreateResult(2, 102);
            var third = CreateResult(3, 103);

            cache.Store(first);
            cache.Store(second);
            Assert.IsTrue(cache.TryGet(first.Signature, out _));

            cache.Store(third);

            Assert.IsTrue(cache.TryGet(first.Signature, out _), "最近使用した entry は維持されること");
            Assert.IsFalse(cache.TryGet(second.Signature, out _), "最も古い entry が eviction されること");
            Assert.IsTrue(cache.TryGet(third.Signature, out _), "新規 entry が保存されること");
        }

        [Test]
        public void Store_WhenFull_ContinuesAcceptingNewEntries()
        {
            var cache = new PreparedDisplayResultCache(3);

            for (int index = 0; index < 8; index++)
                cache.Store(CreateResult(index + 1, 200 + index));

            Assert.AreEqual(3, cache.Count, "capacity 上限を維持したまま登録を継続できること");
            Assert.IsTrue(cache.TryGet(new DisplayResultSignature(207, 1207, ObjectMode.SingleObject), out _),
                "満杯後も最新 entry が取得できること");
        }

        [Test]
        public void RecentDisplayReturn_HitsCacheWithinOneUpdate_AtLeast95Percent()
        {
            var cache = new PreparedDisplayResultCache(4);
            var recentA = CreateResult(1, 301);
            var recentB = CreateResult(2, 302);
            cache.Store(recentA);
            cache.Store(recentB);

            int hitCount = 0;
            const int trials = 100;
            for (int index = 0; index < trials; index++)
            {
                var transient = CreateResult(1000 + index, 400 + index);
                cache.Store(transient);

                if (cache.TryGet(recentA.Signature, out _))
                    hitCount++;

                cache.TryGet(recentB.Signature, out _);
            }

            Assert.GreaterOrEqual(hitCount, 95,
                $"1 回の表示更新以内に戻るケースで cache hit が 95% 未満でした: {hitCount}/{trials}");
        }

        private static PreparedDisplayResult CreateResult(long version, int hashCode)
        {
            return new PreparedDisplayResult
            {
                Version = version,
                Signature = new DisplayResultSignature(hashCode, hashCode + 1000, ObjectMode.SingleObject)
            };
        }
    }
}