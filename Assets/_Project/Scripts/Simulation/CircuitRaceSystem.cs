using System;
using System.Collections.Generic;
using CarFactoryIdle.Data;
using CarFactoryIdle.Events;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Simulation
{
    /// <summary>Kairosoft-style circuit auto-battler. No physics: each car is a single "distance"
    /// value advanced per tick by stat-driven speed. Straights run to top speed; corners cap speed
    /// by handling vs sharpness; aura (Durability) randomly procs a boost; drafting gives a catch-up
    /// bonus so the field stays close. Runs in the background (ticked from GameRoot) so the player
    /// can leave the Race screen and the race keeps going. Reuses VehicleDefinition.baseRaceStats:
    /// topSpeed / acceleration / traction(->handling) / launch(->aura), scaled by the gacha grade.
    ///
    /// Pacing constants are a deliberate first pass — easy to tune once the art/feel pass starts.</summary>
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
                carName = raceCar.displayName + " [RACE]";
                rs = raceCar.baseRaceStats;
                mult = 1f;   // no gacha roll on dedicated race cars
            }
            else
            {
                if (!CarKey.TryParse(carKey, out var vehicleId, out var grade)) return false;
                var v = _cfg.GetVehicle(vehicleId);
                if (v == null) return false;
                carName = v.displayName + " [" + grade.ToKey() + "]";
                rs = v.baseRaceStats;
                mult = _cfg.gachaTable != null ? _cfg.gachaTable.GetMultiplier(grade) : 1f;
            }

            if (track.entryFee > 0 && !_state.wallet.SpendCash(track.entryFee)) return false;
            _bus?.CashChanged(_state.wallet.cash);

            live.entrants.Clear();
            var player = new RaceEntrant
            {
                name = carName,
                isPlayer = true,
                carKey = carKey,
                topSpeed = rs.topSpeed * mult,
                acceleration = rs.acceleration * mult,
                handling = rs.traction * mult,
                aura = rs.launch * mult,
            };
            live.entrants.Add(player);

            float playerBudget = player.topSpeed + player.acceleration + player.handling + player.aura;
            for (int i = 0; i < AiOpponents; i++)
            {
                float band = track.aiStrength * (0.85f + 0.12f * i);   // a staggered field
                float budget = Math.Max(8f, playerBudget * band);
                live.entrants.Add(MakeAi("Rival " + (i + 1), budget));
            }

            live.active = true;
            live.finished = false;
            live.trackIndex = trackIndex;
            live.trackName = track.name;
            live.lapsTotal = track.laps;
            live.lapDistance = track.LapDistance;
            live.elapsed = 0f;
            live.playerPlace = 0;
            live.cashReward = 0; live.trophyReward = 0; live.prestigeReward = 0;
            live.rewardClaimed = false;
            live.resultMessage = "";
            RecomputePlaces();
            return true;
        }

        /// <summary>Fast-forward the running race to its finish.</summary>
        public void SkipToResult()
        {
            var live = _state.raceLive;
            if (!live.active || live.finished) return;
            int guard = 0;
            while (live.active && !live.finished && guard++ < MaxSkipIters)
                Advance(SkipStep);
        }

        /// <summary>Clear a finished race so the lobby shows again (post-race cooldown still applies).</summary>
        public void DismissResult()
        {
            var live = _state.raceLive;
            if (!live.finished) return;
            live.active = false;
            live.finished = false;
            live.entrants.Clear();
            live.resultMessage = "";
        }

        // ---- tick ----

        public void Tick(float dt)
        {
            var live = _state.raceLive;
            if (live.cooldownRemaining > 0f)
                live.cooldownRemaining = Math.Max(0f, live.cooldownRemaining - dt);
            if (!live.active || live.finished) return;
            Advance(dt);
        }

        private void Advance(float dt)
        {
            var live = _state.raceLive;
            var track = RaceCircuits.Get(live.trackIndex);
            if (track == null) { live.finished = true; return; }

            float total = live.lapDistance * live.lapsTotal;
            live.elapsed += dt;

            for (int i = 0; i < live.entrants.Count; i++)
            {
                var e = live.entrants[i];
                if (e.finished) continue;

                float into = e.distance % live.lapDistance;
                var seg = track.SegmentAt(into);
                float maxSpeed = e.topSpeed * SpeedUnit;

                float target;
                if (seg.kind == SegmentKind.Corner)
                {
                    float grip = Clamp(0.45f + 0.006f * e.handling - 0.12f * seg.sharpness, 0.30f, 0.96f);
                    target = maxSpeed * grip;
                }
                else target = maxSpeed;

                if (e.speed < target)
                    e.speed = Math.Min(target, e.speed + e.acceleration * AccelUnit * dt);
                else
                    e.speed = Math.Max(target, e.speed - BrakeRate * dt);

                // aura / boost proc (Durability)
                if (e.auraTimer > 0f)
                {
                    e.auraTimer -= dt;
                    e.auraActive = e.auraTimer > 0f;
                }
                else
                {
                    e.auraActive = false;
                    float chancePerSec = 0.02f + 0.0035f * e.aura;
                    if (_rng.NextDouble() < chancePerSec * dt) { e.auraTimer = AuraDuration; e.auraActive = true; }
                }

                float eff = e.speed;
                if (e.auraActive) eff *= AuraBoostMult;

                // slipstream: drafting a rival just ahead on a straight (catch-up drama)
                if (seg.kind == SegmentKind.Straight)
                {
                    for (int j = 0; j < live.entrants.Count; j++)
                    {
                        if (j == i) continue;
                        float gap = live.entrants[j].distance - e.distance;
                        if (gap > 0f && gap <= SlipstreamRange) { eff *= SlipstreamMult; break; }
                    }
                }

                e.distance += eff * dt;

                int lap = (int)Math.Floor(e.distance / live.lapDistance);
                e.lap = Math.Max(0, Math.Min(lap, live.lapsTotal));

                if (e.distance >= total)
                {
                    e.distance = total;
                    e.finished = true;
                    e.finishTime = live.elapsed;
                    e.speed = 0f;
                    e.auraActive = false;
                }
            }

            RecomputePlaces();

            bool allDone = true;
            for (int i = 0; i < live.entrants.Count; i++)
                if (!live.entrants[i].finished) { allDone = false; break; }
            if (allDone) FinalizeRace();
        }

        private void RecomputePlaces()
        {
            var live = _state.raceLive;
            var order = new List<RaceEntrant>(live.entrants);
            order.Sort((a, b) =>
            {
                if (a.finished && b.finished) return a.finishTime.CompareTo(b.finishTime);
                if (a.finished) return -1;
                if (b.finished) return 1;
                return b.distance.CompareTo(a.distance);
            });
            for (int i = 0; i < order.Count; i++) order[i].place = i + 1;
        }

        private void FinalizeRace()
        {
            var live = _state.raceLive;
            live.finished = true;
            RecomputePlaces();

            var track = RaceCircuits.Get(live.trackIndex);
            int place = 1;
            float playerTime = live.elapsed;
            for (int i = 0; i < live.entrants.Count; i++)
                if (live.entrants[i].isPlayer) { place = live.entrants[i].place; playerTime = live.entrants[i].finishTime; break; }
            live.playerPlace = place;

            float factor = place == 1 ? 1f : place == 2 ? 0.5f : place == 3 ? 0.25f : 0.1f;
            long cash = (long)Math.Floor((track != null ? track.cashReward : 0) * factor);
            int trophies = place == 1 ? (track != null ? track.trophyReward : 0)
                         : place == 2 ? Math.Max(0, (track != null ? track.trophyReward : 0) / 2)
                         : 0;
            int prestige = place == 1 ? (track != null ? track.prestigeReward : 0) : 0;

            live.cashReward = cash; live.trophyReward = trophies; live.prestigeReward = prestige;
            if (cash > 0) _state.wallet.AddCash(cash);
            if (trophies > 0) _state.wallet.AddTrophies(trophies);
            if (prestige > 0) _state.prestigePoints += prestige;
            live.cooldownRemaining = PostRaceCooldown;
            _state.racesCompleted++;
            live.resultMessage = place == 1 ? "\uD83C\uDFC6 WON!" : "Finished " + Ordinal(place);

            var ev = new RaceResultEvent(place == 1, cash, trophies, playerTime, 0f);
            _bus?.raceFinished?.Raise(ev);
            _bus?.CashChanged(_state.wallet.cash);
            if (trophies > 0) _bus?.TrophiesChanged(_state.wallet.trophies);
        }

        // ---- helpers ----

        private RaceEntrant MakeAi(string name, float budget)
        {
            float Quarter() => budget / 4f * (0.85f + (float)_rng.NextDouble() * 0.30f);
            return new RaceEntrant
            {
                name = name,
                isPlayer = false,
                topSpeed = Quarter(),
                acceleration = Quarter(),
                handling = Quarter(),
                aura = Quarter(),
            };
        }

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);

        private static string Ordinal(int n) => n switch
        {
            1 => "1st", 2 => "2nd", 3 => "3rd", _ => n + "th"
        };
    }
}
