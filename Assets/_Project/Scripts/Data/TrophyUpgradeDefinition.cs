using UnityEngine;

namespace CarFactoryIdle.Data
{
    [CreateAssetMenu(menuName = "CFI/Trophy Upgrade Definition", fileName = "TrophyUpgrade")]
    public class TrophyUpgradeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public int trophyCost;
        [TextArea] public string effectDescription;
    }
}
