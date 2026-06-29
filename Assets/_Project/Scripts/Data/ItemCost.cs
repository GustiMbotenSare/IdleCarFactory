using System;

namespace CarFactoryIdle.Data
{
    /// <summary>A quantity of an item (raw material or component) by string id.</summary>
    [Serializable]
    public struct ItemCost
    {
        public string itemId;
        public int quantity;

        public ItemCost(string itemId, int quantity)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }
    }
}
