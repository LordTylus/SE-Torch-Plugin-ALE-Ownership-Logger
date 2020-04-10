using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALE_Ownership_Logger {
    public class Cache {

        private readonly ConcurrentDictionary<long, CacheItem> _cache = new ConcurrentDictionary<long, CacheItem>();

        public void Store(long blockId, ChangingEntity entity, TimeSpan expiresAfter) {
            _cache[blockId] = new CacheItem(entity, expiresAfter);
        }

        public ChangingEntity Get(long key) {

            CleanCache();

            CacheItem entry = null;
            _cache.TryGetValue(key, out entry);

            if (entry == null)
                return null;

            return entry.Value;
        }

        private void CleanCache() {

            var keys = new List<long>(_cache.Keys);

            foreach (long key in keys) {

                CacheItem entry = null;
                _cache.TryGetValue(key, out entry);

                if (entry == null)
                    continue;

                if (DateTimeOffset.Now - entry.Created >= entry.ExpiresAfter) 
                    _cache.TryRemove(key, out entry);
            }
        }

        public class CacheItem {

            public CacheItem(ChangingEntity entity, TimeSpan expiresAfter) {
                Value = entity;
                ExpiresAfter = expiresAfter;
            }

            public ChangingEntity Value { get; }
            internal DateTimeOffset Created { get; } = DateTimeOffset.Now;
            internal TimeSpan ExpiresAfter { get; }
        }
    }
}
