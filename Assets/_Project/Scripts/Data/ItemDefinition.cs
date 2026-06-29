using UnityEngine;

namespace CarFactoryIdle.Data
{
    public enum ItemKind { Raw, Component }

    /// <summary>Static config for a raw material or crafted component. Display/icon only;
    /// the simulation references items by string id.</summary>
    [CreateAssetMenu(menuName = "CFI/Item Definition", fileName = "Item")]
    public class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string icon;        // emoji or sprite key
        public ItemKind kind;
        public int tier = 1;       // 1-3 for components
    }
}
