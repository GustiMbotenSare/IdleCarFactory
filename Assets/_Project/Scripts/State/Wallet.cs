using System;

namespace CarFactoryIdle.State
{
    [Serializable]
    public class Wallet
    {
        public long cash;
        public long trophies;

        public bool CanAfford(long amount) => cash >= amount;

        public bool SpendCash(long amount)
        {
            if (cash < amount) return false;
            cash -= amount;
            return true;
        }

        public void AddCash(long amount) => cash += amount;

        public bool SpendTrophies(long amount)
        {
            if (trophies < amount) return false;
            trophies -= amount;
            return true;
        }

        public void AddTrophies(long amount) => trophies += amount;
    }
}
