using UnityEngine;

namespace CarFactoryIdle.Data
{
    public enum ContractType { Standard, Bulk, Rush, Premium, VIP }

    /// <summary>Config for a contract archetype. Concrete contracts are rolled at runtime
    /// (see ContractSystem) using these ranges + the current factory tier.</summary>
    [CreateAssetMenu(menuName = "CFI/Contract Type Definition", fileName = "ContractType")]
    public class ContractTypeDefinition : ScriptableObject
    {
        public ContractType type;
        public string displayName;
        public int minFactoryTier = 1;

        [Header("Quantity = Random(minQty, maxQtyBase + factoryTier * maxQtyPerTier)")]
        public int minQty = 1;
        public int maxQtyBase = 1;
        public int maxQtyPerTier = 0;

        [Header("Cash = payoutMultiplier * basePrice * qty (0 = trophy-only)")]
        public float payoutMultiplier = 1.0f;

        [Header("Timer = baseTimerSeconds + factoryTier * timerPerTier (0 = no timer)")]
        public float baseTimerSeconds = 0f;
        public float timerPerTier = 0f;

        [Header("Trophy reward = Random(trophyMin, trophyMax) + factoryTier * trophyPerTier")]
        public int trophyMin = 0;
        public int trophyMax = 0;
        public int trophyPerTier = 0;
    }
}
