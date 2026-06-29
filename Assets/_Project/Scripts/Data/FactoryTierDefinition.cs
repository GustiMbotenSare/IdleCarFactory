using UnityEngine;

namespace CarFactoryIdle.Data
{
    [CreateAssetMenu(menuName = "CFI/Factory Tier Definition", fileName = "FactoryTier")]
    public class FactoryTierDefinition : ScriptableObject
    {
        public int tier = 1;
        public string displayName;
        public long cost;
        public float productionMultiplier = 1f;
    }
}
