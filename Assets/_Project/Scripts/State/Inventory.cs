using System;
using System.Collections.Generic;
using UnityEngine;
using CarFactoryIdle.Data;

namespace CarFactoryIdle.State
{
    /// <summary>Flat string-keyed counters for raw materials, components, and graded cars
    /// (e.g. \"steel\", \"engineV8\", \"tokyoCommuter_S\"). Uses parallel lists for JsonUtility
    /// serialization while keeping an O(1) dictionary at runtime.</summary>
    [Serializable]
    public class Inventory : ISerializationCallbackReceiver
    {
        [SerializeField] private List<string> _keys = new();
        [SerializeField] private List<long> _values = new();

        private readonly Dictionary<string, long> _map = new();

        public long Get(string key) => _map.TryGetValue(key, out var v) ? v : 0;

        public void Add(string key, long amount)
        {
            if (amount == 0) return;
            long next = Get(key) + amount;
            if (next <= 0) _map.Remove(key);
            else _map[key] = next;
        }

        public bool TryConsume(string key, long amount)
        {
            if (Get(key) < amount) return false;
            Add(key, -amount);
            return true;
        }

        public bool CanAfford(IList<ItemCost> costs)
        {
            for (int i = 0; i < costs.Count; i++)
                if (Get(costs[i].itemId) < costs[i].quantity) return false;
            return true;
        }

        public bool TrySpend(IList<ItemCost> costs)
        {
            if (!CanAfford(costs)) return false;
            for (int i = 0; i < costs.Count; i++)
                Add(costs[i].itemId, -costs[i].quantity);
            return true;
        }

        public IReadOnlyDictionary<string, long> All => _map;

        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();
            foreach (var kv in _map)
            {
                _keys.Add(kv.Key);
                _values.Add(kv.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            _map.Clear();
            for (int i = 0; i < _keys.Count && i < _values.Count; i++)
                _map[_keys[i]] = _values[i];
        }
    }
}
