using System;
using System.Collections.Generic;
using CarFactoryIdle.Data;

namespace CarFactoryIdle.Simulation
{
    /// <summary>Weighted Gacha grade roll. Deterministic given the injected RNG (good for
    /// reproducible offline fast-forward and tests).</summary>
    public class GachaRoller
    {
        private readonly Random _rng;
        public GachaRoller(Random rng) { _rng = rng ?? new Random(); }

        public Grade Roll(GachaTable table, bool qualityControl)
        {
            IReadOnlyList<GachaTable.Entry> entries = table.Table(qualityControl);
            float total = 0f;
            for (int i = 0; i < entries.Count; i++) total += entries[i].weight;
            double r = _rng.NextDouble() * total;
            float acc = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                acc += entries[i].weight;
                if (r <= acc) return entries[i].grade;
            }
            return entries.Count > 0 ? entries[entries.Count - 1].grade : Grade.C;
        }
    }
}
