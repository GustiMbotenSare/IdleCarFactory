using System;
using UnityEngine;
using CarFactoryIdle.Data;
using CarFactoryIdle.Events;
using CarFactoryIdle.Platform;
using CarFactoryIdle.Simulation;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Core
{
    /// <summary>The single bootstrap MonoBehaviour. Owns the pure-C# systems and ticks them in a
    /// fixed, deterministic order. Presentation never reaches into the systems; it listens to the
    /// GameEventBus. This is what keeps logic and UI decoupled.</summary>
    public class GameRoot : MonoBehaviour
    {
        [Tooltip("Optional. If empty, DefaultContent builds the full config in code.")]
        public GameConfig config;
        [Tooltip("Optional. If empty, headless raises are skipped (still fully playable).")]
        public GameEventBus bus;
        public CrazyAds ads;

        [Header("Runtime (read-only)")]
        public float autosaveInterval = 30f;

        public GameState State { get; private set; }
        public ProductionSystem Production { get; private set; }
        public AssemblySystem Assembly { get; private set; }
        public AuctionSystem Auction { get; private set; }
        public ContractSystem Contracts { get; private set; }
        public ShowroomSystem Showroom { get; private set; }
        public RaceSystem Race { get; private set; }
        public CircuitRaceSystem Circuit { get; private set; }
        public RaceWorkshopSystem Workshop { get; private set; }
        public OfflineService Offline { get; private set; }
        public GameFacade Facade { get; private set; }

        /// <summary>Set once on load when meaningful offline progress was credited; consumed by the
        /// Welcome Back modal (UI). Null until/unless there is something worth showing.</summary>
        public OfflineReport PendingOfflineReport { get; private set; }

        /// <summary>Below this, offline progress isn't worth interrupting the player with a modal.</summary>
        public const float OfflineModalMinSeconds = 60f;

        private float _autosaveTimer;
        private System.Random _rng;

        private void Awake()
        {
            if (config == null) config = DefaultContent.BuildConfig();
            config.Init();
            _rng = new System.Random();

            //State = SaveSystem.Load() ?? NewGame();
            State = NewGame();

            var roller = new GachaRoller(_rng);
            Production = new ProductionSystem(config, State, bus);
            Assembly = new AssemblySystem(config, State, bus, roller);
            Auction = new AuctionSystem(config, State, bus, _rng);
            Contracts = new ContractSystem(config, State, bus, _rng);
            Race = new RaceSystem(config, State, bus, _rng);
            Circuit = new CircuitRaceSystem(config, State, bus, _rng);
            Workshop = new RaceWorkshopSystem(config, State, bus);
            Showroom = new ShowroomSystem(config, State, bus, Auction, _rng);
            Offline = new OfflineService(config, State, Production, Assembly);
            Facade = new GameFacade(config, State, bus, Production, Assembly, Auction, Contracts, Showroom, Circuit, Workshop);

            Showroom.EnsureInitialized();
            ApplyOfflineProgress();
            if (State.contracts.Count == 0) Contracts.Refresh();

            if (ads != null) { ads.Init(); ads.GameplayStart(); }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Production.Tick(dt);   // 1. extractors + manufacturing
            Assembly.Tick(dt);     // 2. selected vehicle assembly + gacha
            Auction.Tick(dt);      // 3. active auction bids / settlement
            Showroom.Tick(dt);     // 4. walk-in offers + auto-display newest car
            Contracts.Tick(dt);    // 5. rush timers + 120s refresh
            Race.Tick(dt);         // 6. legacy drag-race cooldown
            Circuit.Tick(dt);      // 7. live circuit race (runs even off-screen)

            _autosaveTimer += dt;
            if (_autosaveTimer >= autosaveInterval) { _autosaveTimer = 0f; SaveSystem.Save(State); }
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveSystem.Save(State); }
        private void OnApplicationQuit() => SaveSystem.Save(State);

        private void ApplyOfflineProgress()
        {
            if (State.lastSaveUnixSeconds <= 0) return;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long away = now - State.lastSaveUnixSeconds;
            if (away <= 0) return;
            var report = Offline.Apply(away);
            Debug.Log($"[Offline] {report.secondsCredited:0}s credited | raw {report.rawProduced}, " +
                      $"parts {report.componentsProduced}, cars {report.carsAssembled}");
            // Surface to the UI Welcome Back modal only when there's something worth showing.
            bool produced = report.rawProduced > 0 || report.componentsProduced > 0 || report.carsAssembled > 0;
            if (report.secondsCredited >= OfflineModalMinSeconds && produced)
                PendingOfflineReport = report;
        }

        /// <summary>Fresh save: unlock extractors + tier-1 manufacturing/assembly + sales so the loop
        /// runs. Automation starts OFF (player buys it); extractors can be manually tapped.</summary>
        public GameState NewGame()
        {
            var s = new GameState();
            s.wallet.AddCash(10000);
            foreach (var def in config.stations)
            {
                s.stations.Add(new StationState
                {
                    definitionId = def.id,
                    unlocked = def.unlockedByDefault,
                    automated = false
                });
            }
            s.selectedVehicleId = "tokyoCommuter";
            s.contractRefreshTimer = ContractSystem.RefreshSeconds;
            return s;
        }

        // ---- Player intents (called by UI) ----
        public bool TapStation(string id) => Production.ManualTap(id);
        public bool BuildOneManually()
        {
            var v = config.GetVehicle(State.selectedVehicleId);
            return v != null && Assembly.AssembleOnce(v);
        }
        public bool PutOnAuction(string carKey) => Auction.StartAuction(carKey);
        public void InstantSell() => Auction.InstantSell();
    }
}
