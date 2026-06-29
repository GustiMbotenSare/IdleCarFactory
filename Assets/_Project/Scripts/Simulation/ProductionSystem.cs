using CarFactoryIdle.Data;
using CarFactoryIdle.Events;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Simulation
{
    /// <summary>Ticks Extractor + Manufacturing stations. Pure logic; no scene refs.</summary>
    public class ProductionSystem
    {
        private readonly GameConfig _cfg;
        private readonly GameState _state;
        private readonly GameEventBus _bus;

        public ProductionSystem(GameConfig cfg, GameState state, GameEventBus bus)
        { _cfg = cfg; _state = state; _bus = bus; }

        public void Tick(float dt)
        {
            float fmult = _cfg.ProductionMultiplier(_state.factoryTierIndex);
            for (int i = 0; i < _state.stations.Count; i++)
            {
                var ss = _state.stations[i];
                var def = _cfg.GetStation(ss.definitionId);
                if (def == null || !ss.unlocked || !ss.automated) continue;
                if (def.category != StationCategory.Extractor && def.category != StationCategory.Manufacturing) continue;
                RunStation(ss, def, dt, fmult);
            }
        }

        private void RunStation(StationState ss, StationDefinition def, float dt, float fmult)
        {
            float interval = Economy.EffectiveInterval(def.baseIntervalSeconds, ss.speedLevel, fmult);
            ss.progress += dt;
            // Guard against runaway loops on huge dt (offline uses a dedicated path).
            int safety = 100000;
            while (ss.progress >= interval && safety-- > 0)
            {
                if (!ProduceOnce(ss, def)) { ss.progress = 0f; break; }
                ss.progress -= interval;
            }
        }

        /// <summary>Runs one cycle if inputs are affordable. Returns false if blocked.</summary>
        public bool ProduceOnce(StationState ss, StationDefinition def)
        {
            if (def.inputs.Count > 0 && !_state.inventory.TrySpend(def.inputs)) return false;
            int output = Economy.EffectiveOutput(def.baseOutput, ss.capacityLevel);
            _state.inventory.Add(def.outputItemId, output);
            return true;
        }

        /// <summary>Manual tap. Works on extractors even when automated (GDD bonus rule).</summary>
        public bool ManualTap(string stationId)
        {
            var ss = _state.GetStation(stationId);
            var def = _cfg.GetStation(stationId);
            if (ss == null || def == null || !ss.unlocked) return false;
            bool allowed = def.category == StationCategory.Extractor || !ss.automated;
            if (!allowed) return false;
            return ProduceOnce(ss, def);
        }
    }
}
