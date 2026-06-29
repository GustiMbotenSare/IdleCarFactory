using CarFactoryIdle.Data;
using CarFactoryIdle.Events;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Simulation
{
    /// <summary>Builds the currently selected vehicle on the assembly station whose tier matches
    /// the vehicle. Rolls the Gacha grade on completion (also runs offline).</summary>
    public class AssemblySystem
    {
        private readonly GameConfig _cfg;
        private readonly GameState _state;
        private readonly GameEventBus _bus;
        private readonly GachaRoller _roller;

        public AssemblySystem(GameConfig cfg, GameState state, GameEventBus bus, GachaRoller roller)
        { _cfg = cfg; _state = state; _bus = bus; _roller = roller; }

        public void Tick(float dt)
        {
            var vehicle = _cfg.GetVehicle(_state.selectedVehicleId);
            if (vehicle == null) return;

            float fmult = _cfg.ProductionMultiplier(_state.factoryTierIndex);
            for (int i = 0; i < _state.stations.Count; i++)
            {
                var ss = _state.stations[i];
                var def = _cfg.GetStation(ss.definitionId);
                if (def == null || def.category != StationCategory.Assembly) continue;
                if (!ss.unlocked || !ss.automated || def.tier != vehicle.tier) continue;

                float interval = Economy.EffectiveInterval(def.baseIntervalSeconds, ss.speedLevel, fmult);
                ss.progress += dt;
                int safety = 100000;
                while (ss.progress >= interval && safety-- > 0)
                {
                    if (!AssembleOnce(vehicle)) { ss.progress = 0f; break; }
                    ss.progress -= interval;
                }
            }
        }

        /// <summary>Consumes the recipe and produces one graded car. Returns false if blocked.</summary>
        public bool AssembleOnce(VehicleDefinition vehicle)
        {
            if (!_state.inventory.TrySpend(vehicle.recipe)) return false;
            Grade grade = _roller.Roll(_cfg.gachaTable, _state.qualityControlOwned);
            string key = CarKey.Build(vehicle.id, grade);
            _state.inventory.Add(key, 1);
            _state.lastAssembledCarKey = key;
            _bus?.carAssembled?.Raise(new CarAssembledEvent(vehicle.id, grade, key));
            if (grade == Grade.S || grade == Grade.SPlus)
                _bus?.Milestone("rare_assembly_" + grade.ToKey());
            return true;
        }
    }
}
