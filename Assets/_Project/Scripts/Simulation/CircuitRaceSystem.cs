using System;
using System.Collections.Generic;
using CarFactoryIdle.Data;
using CarFactoryIdle.Events;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Simulation
{
    /// <summary>An arcade-style circuit auto-racer. No physics: each car is a single "distance"
    /// value advanced per tick by stat-driven speed. Straights run to top speed; corners cap speed
    /// by handling vs sharpness; aura (Durability) randomly procs a boost; drafting gives a catch-up
    /// bonus so the field stays close. Runs in the background (ticked from GameRoot) so the player
    /// can leave the Race screen and the race keeps going. Reuses VehicleDefinition.baseRaceStats:
    /// topSpeed / acceleration / traction(->handling) / launch(->aura), scaled by the gacha grade.
    ///
    /// Pacing constants are a deliberate first pass and easy to tune once the art and feel pass starts.</summary>
    public class CircuitRaceSystem
    {
        public const string RaceAgencyId = "raceAgency";

        // ---- tuning (abstract units) ----
        public const float SpeedUnit = 7.0f;        // stat topSpeed -> unit speed
        public const float AccelUnit = 2.0f;        // stat acceleration -> speed gain / sec
        public const float BrakeRate = 80f;         // speed shed / sec when over a corner cap
        public const float AuraBoostMult = 1.35f;   // speed multiplier while a boost is active
        public const float AuraDuration = 1.5f;     // seconds per aura proc
        public const float SlipstreamRange = 30f;   // meters behind a rival for a draft bonus
        public const float SlipstreamMult = 1.08f;
        public const float PostRaceCooldown = 10f;  // seconds before the next race can start
        public const float SkipStep = 0.1f;         // fixed dt used when fast-forwarding to result
        public const int MaxSkipIters = 50000;      // safety cap on the skip loop
        private const int AiOpponents = 3;

        private readonly GameConfig _cfg;
        private readonly GameState _state;
        private readonly GameEventBus _bus;
        private readonly Random _rng;

        public CircuitRaceSystem(GameConfig cfg, GameState state, GameEventBus bus, Random rng)
        { _cfg = cfg; _state = state; _bus = bus; _rng = rng ?? new Random(); }

        public RaceState Live => _state.raceLive;
        public bool Unlocked => _state.ownedSpecialUnlocks.Contains(RaceAgencyId);

        // ---- intents ----

        /// <summary>Starts a race with one of the player's owned cars vs AI rivals. The car is NOT
        /// consumed (it races, it isn't sold). Charges the track entry fee. Atomic.</summary>
        public bool StartRace(string carKey, int trackIndex)
        {
            var live = _state.raceLive;
            if (!Unlocked) return false;
            if (live.active && !live.finished) return false;   // a race is already running
            if (live.cooldownRemaining > 0f) return false;

            var track = RaceCircuits.Get(trackIndex);
            if (track == null) return false;
            if (string.IsNullOrEmpty(carKey) || _state.inventory.Get(carKey) <= 0) return false;
            // Resolve the entry: a dedicated race car (flat engineered stats) OR a graded showroom car.
            string carName;
            RaceStats rs;
            float mult;
            var raceCar = _cfg.GetRaceCar(carKey);
            if (raceCar != null)
            {
                carName = raceCar.displayName