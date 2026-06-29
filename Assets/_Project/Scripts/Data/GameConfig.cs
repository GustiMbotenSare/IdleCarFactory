using System.Collections.Generic;
using UnityEngine;

namespace CarFactoryIdle.Data
{
    /// <summary>The authored content database. One asset references every definition.
    /// Call Init() once before use to build id lookups.</summary>
    [CreateAssetMenu(menuName = "CFI/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public List<ItemDefinition> items = new();
        public List<StationDefinition> stations = new();
        public List<VehicleDefinition> vehicles = new();
        public List<VehicleDefinition> raceCars = new();   // dedicated race-only cars (no gacha grade)
        public List<FactoryTierDefinition> factoryTiers = new();
        public List<ContractTypeDefinition> contractTypes = new();
        public List<TrophyUpgradeDefinition> trophyUpgrades = new();
        public GachaTable gachaTable;

        public const long QualityControlCost = 25000;

        private Dictionary<string, StationDefinition> _stationById;
        private Dictionary<string, VehicleDefinition> _vehicleById;
        private Dictionary<string, VehicleDefinition> _raceCarById;
        private Dictionary<string, ItemDefinition> _itemById;

        public void Init()
        {
            _stationById = new();
            foreach (var s in stations) if (s != null) _stationById[s.id] = s;
            _vehicleById = new();
            foreach (var v in vehicles) if (v != null) _vehicleById[v.id] = v;
            _raceCarById = new();
            foreach (var rc in raceCars) if (rc != null) _raceCarById[rc.id] = rc;
            _itemById = new();
            foreach (var i in items) if (i != null) _itemById[i.id] = i;
        }

        public StationDefinition GetStation(string id)
            => _stationById != null && _stationById.TryGetValue(id, out var s) ? s : null;

        public VehicleDefinition GetVehicle(string id)
            => _vehicleById != null && _vehicleById.TryGetValue(id, out var v) ? v : null;

        public VehicleDefinition GetRaceCar(string id)
            => _raceCarById != null && id != null && _raceCarById.TryGetValue(id, out var rc) ? rc : null;

        public ItemDefinition GetItem(string id)
            => _itemById != null && _itemById.TryGetValue(id, out var i) ? i : null;

        public FactoryTierDefinition GetFactoryTier(int index)
            => (index >= 0 && index < factoryTiers.Count) ? factoryTiers[index] : null;

        public float ProductionMultiplier(int tierIndex)
        {
            var t = GetFactoryTier(tierIndex);
            return t != null ? t.productionMultiplier : 1f;
        }
    }
}
