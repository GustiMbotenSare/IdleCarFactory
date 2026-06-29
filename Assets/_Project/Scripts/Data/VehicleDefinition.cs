using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarFactoryIdle.Data
{
    [Serializable]
    public struct RaceStats
    {
        public float topSpeed;
        public float acceleration;
        public float traction;
        public float launch;
    }

    /// <summary>One of the 12 trademark-safe vehicles.</summary>
    [CreateAssetMenu(menuName = "CFI/Vehicle Definition", fileName = "Vehicle")]
    public class VehicleDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public int tier = 1;
        public List<ItemCost> recipe = new();
        public long basePrice;
        public RaceStats baseRaceStats;
    }
}
