using System;
using CarFactoryIdle.Data;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Simulation
{
    /// <summary>Fast-forwards the full pipeline on load. Runs extractors, then manufacturing +
    /// assembly (which DO roll Gacha). Cap 8h, 25% efficiency, 200/raw extractor cap. No auto-sell.</summary>
    public class OfflineService
    {
        public const float MaxOfflineSeconds = 28800f; // 8 hours
        public const float Efficiency = 0.25f;          // 25% rate
        public const long ExtractorCapPerType = 200;

        private readonly GameConfig _cfg;
        private readonly GameState _state;
        private readonly ProductionSystem _production;
        private readonly AssemblySystem _assembly;

        public OfflineService(GameConfig cfg, GameState state, ProductionSystem production, AssemblySystem assembly)
        { _cfg = cfg; _state = state; _production = production; _assembly = assembly; }

        public OfflineReport Apply(long secondsAway)
        {
            var report = new OfflineReport();
            float effective = Math.Min(secondsAway, MaxOfflineSeconds);
            report.secondsCredited = effective;
            if (effective <= 0f) return report;

            float fmult = _cfg.ProductionMultiplier(_state.factoryTierIndex);

            // Pass 1: extractors (capped).
            foreach (var ss in _state.stations)
            {
                var def = _cfg.GetStation(ss.definitionId);
                if (def == null || def.category != StationCategory.Extractor) continue;
                if (!ss.unlocked || !ss.automated) continue;
                float interval = Economy.EffectiveInterval(def.baseIntervalSeconds, ss.speedLevel, fmult);
                long cycles = (long)Math.Floor(effective * Efficiency / Math.Max(0.0001f, interval));
                long perCycle = Economy.EffectiveOutput(def.baseOutput, ss.capacityLevel);
                long produced = cycles * perCycle;
                long current = _state.inventory.Get(def.outputItemId);
                long capped = Math.Min(produced, Math.Max(0, ExtractorCapPerType - current));
                _state.inventory.Add(def.outputItemId, capped);
                report.rawProduced += capped;
            }

            // Pass 2: manufacturing + assembly, input-limited (greedy, capped iterations).
            foreach (var ss in _state.stations)
            {
                var def = _cfg.GetStation(ss.definitionId);
                if (def == null || !ss.unlocked || !ss.automated) continue;
                if (def.category != StationCategory.Manufacturing) continue;
                float interval = Economy.EffectiveInterval(def.baseIntervalSeconds, ss.speedLevel, fmult);
                long maxCycles = (long)Math.Floor(effective * Efficiency / Math.Max(0.0001f, interval));
                for (long c = 0; c < maxCycles; c++)
                    if (!_production.ProduceOnce(ss, def)) break;
                    else report.componentsProduced += Economy.EffectiveOutput(def.baseOutput, ss.capacityLevel);
            }

            var vehicle = _cfg.GetVehicle(_state.selectedVehicleId);
            if (vehicle != null)
            {
                foreach (var ss in _state.stations)
                {
                    var def = _cfg.GetStation(ss.definitionId);
                    if (def == null || def.category != StationCategory.Assembly) continue;
                    if (!ss.unlocked || !ss.automated || def.tier != vehicle.tier) continue;
                    float interval = Economy.EffectiveInterval(def.baseIntervalSeconds, ss.speedLevel, fmult);
                    long maxCycles = (long)Math.Floor(effective * Efficiency / Math.Max(0.0001f, interval));
                    for (long c = 0; c < maxCycles; c++)
                        if (!_assembly.AssembleOnce(vehicle)) break;
                        else report.carsAssembled++;
                }
            }

            return report;
        }
    }

    public class OfflineReport
    {
        public float secondsCredited;
        public long rawProduced;
        public long componentsProduced;
        public long carsAssembled;
    }
}
