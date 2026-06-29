using System.Collections.Generic;
using UnityEngine;

namespace CarFactoryIdle.Data
{
    public enum StationCategory { Extractor, Manufacturing, Assembly, Sales }

    /// <summary>Immutable config for one factory station. Authored as a .asset and never
    /// mutated at runtime (mutable progress lives in StationState).</summary>
    [CreateAssetMenu(menuName = "CFI/Station Definition", fileName = "Station")]
    public class StationDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public StationCategory category;
        [Tooltip("Manufacturing: 1-3. Assembly: 1-4 (matches vehicle tier built here).")]
        public int tier = 1;

        [Tooltip("Consumed each cycle. Raw ids (steel/rubber/silicon) and/or component ids.")]
        public List<ItemCost> inputs = new();

        [Tooltip("Component/material produced. Ignored for Assembly (uses selected vehicle) and Sales.")]
        public string outputItemId;

        public int baseOutput = 1;
        public float baseIntervalSeconds = 2f;
        public long automationCost = 1000;
        public bool unlockedByDefault = true;
        public long unlockCost = 0;
    }
}
