using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarFactoryIdle.Data
{
    /// <summary>Gacha grade weights + price multipliers. Holds both the base table and the
    /// post-\"Quality Control\" table (one-time $25k upgrade).</summary>
    [CreateAssetMenu(menuName = "CFI/Gacha Table", fileName = "GachaTable")]
    public class GachaTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public Grade grade;
            public float weight;          // relative weight (percent points are fine)
            public float priceMultiplier; // applied to vehicle base price
        }

        public List<Entry> baseEntries = new();
        public List<Entry> qualityControlEntries = new();

        public float GetMultiplier(Grade grade)
        {
            foreach (var e in baseEntries)
                if (e.grade == grade) return e.priceMultiplier;
            return 1f;
        }

        public IReadOnlyList<Entry> Table(bool qualityControl)
            => qualityControl ? qualityControlEntries : baseEntries;
    }
}
