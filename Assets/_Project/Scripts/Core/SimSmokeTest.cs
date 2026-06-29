using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CarFactoryIdle.Data;
using CarFactoryIdle.Simulation;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Core
{
    /// <summary>Phase 1 deliverable: proves raw -> parts -> graded car -> auction -> cash works
    /// deterministically without any UI. Drop on an empty GameObject and press Play, or right-click
    /// the component and choose "Run Headless Sim".
    ///
    /// IMPORTANT: it only automates the stations in the SELECTED vehicle's dependency chain (plus its
    /// matching assembly line + sales). Automating the WHOLE factory starves a tier-1 car, because the
    /// V6/V8 plants and Alloy/Carbon weavers eat the basic engines and steel chassis the assembly needs.</summary>
    public class SimSmokeTest : MonoBehaviour
    {
        [Tooltip("Which vehicle to build. Its supply chain is auto-enabled.")]
        public string vehicleId = "tokyoCommuter";
        [Tooltip("How many seconds of game time to simulate.")]
        public int simulatedSeconds = 180;
        [Tooltip("Simulation step size. Smaller = more accurate, slower.")]
        public float stepDt = 0.1f;
        [Tooltip("Fixed seed = identical run every time. Change for variety.")]
        public int randomSeed = 12345;
        [Tooltip("Auto-list assembled cars on the auction so we can watch cash come in.")]
        public bool autoSellCars = true;

        private void Start() => RunHeadless();

        [ContextMenu("Run Headless Sim")]
        public void RunHeadless()
        {
            var cfg = DefaultContent.BuildConfig();
            cfg.Init();
            var state = NewGameForVehicle(cfg, vehicleId);
            var rng = new System.Random(randomSeed);
            var roller = new GachaRoller(rng);
            var production = new ProductionSystem(cfg, state, null);
            var assembly = new AssemblySystem(cfg, state, null, roller);
            var auction = new AuctionSystem(cfg, state, null, rng);

            int steps = Mathf.Max(1, Mathf.RoundToInt(simulatedSeconds / stepDt));
            for (int i = 0; i < steps; i++)
            {
                production.Tick(stepDt);
                assembly.Tick(stepDt);
                auction.Tick(stepDt);

                if (autoSellCars && !state.auction.active)
                {
                    foreach (var kv in state.inventory.All)
                    {
                        if (kv.Value > 0 && CarKey.TryParse(kv.Key, out _, out _))
                        { auction.StartAuction(kv.Key); break; }
                    }
                }
            }

            Debug.Log(BuildReport(cfg, state));
        }

        private string BuildReport(GameConfig cfg, GameState state)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== SmokeTest: {simulatedSeconds}s simulated, building '{vehicleId}' (seed {randomSeed}) ===");
            sb.AppendLine($"CASH: ${state.wallet.cash:n0}   TROPHIES: {state.wallet.trophies}");
            sb.AppendLine("-- Automated stations --");
            foreach (var ss in state.stations)
                if (ss.automated) sb.AppendLine($"  {ss.definitionId}");
            sb.AppendLine("-- Raw / components on hand --");
            foreach (var item in cfg.items)
            {
                long n = state.inventory.Get(item.id);
                if (n > 0) sb.AppendLine($"  {item.displayName} ({item.id}): {n}");
            }
            sb.AppendLine("-- Cars assembled (by grade) --");
            bool anyCar = false;
            foreach (var kv in state.inventory.All)
                if (kv.Value > 0 && CarKey.TryParse(kv.Key, out _, out _))
                { sb.AppendLine($"  {kv.Key}: {kv.Value}"); anyCar = true; }
            if (!anyCar) sb.AppendLine("  (none yet. Try more simulatedSeconds or add a second extractor.)");
            return sb.ToString();
        }

        /// <summary>Unlocks + automates only what the chosen vehicle needs: its recipe's component
        /// stations (transitively), the extractors feeding them, the matching-tier assembly line,
        /// and the sales office.</summary>
        private GameState NewGameForVehicle(GameConfig cfg, string id)
        {
            var vehicle = cfg.GetVehicle(id) ?? cfg.vehicles[0];
            var s = new GameState { selectedVehicleId = vehicle.id };

            // 1. Compute the transitive set of item ids this vehicle needs.
            var needed = new HashSet<string>();
            foreach (var c in vehicle.recipe) needed.Add(c.itemId);
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var st in cfg.stations)
                {
                    if (string.IsNullOrEmpty(st.outputItemId) || !needed.Contains(st.outputItemId)) continue;
                    foreach (var inp in st.inputs)
                        if (needed.Add(inp.itemId)) changed = true;
                }
            }

            // 2. Enable only the stations that participate in that chain.
            foreach (var def in cfg.stations)
            {
                bool on = def.category switch
                {
                    StationCategory.Extractor     => needed.Contains(def.outputItemId),
                    StationCategory.Manufacturing => !string.IsNullOrEmpty(def.outputItemId) && needed.Contains(def.outputItemId),
                    StationCategory.Assembly      => def.tier == vehicle.tier,
                    StationCategory.Sales         => true,
                    _                             => false,
                };
                s.stations.Add(new StationState { definitionId = def.id, unlocked = on, automated = on });
            }
            return s;
        }
    }
}
