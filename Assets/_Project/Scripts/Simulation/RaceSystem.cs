using System;
using CarFactoryIdle.Data;
using CarFactoryIdle.Events;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Simulation
{
    /// <summary>Legacy drag-race resolution. Encodes the locked race specs: 402m track, 180s
    /// cooldown, launch quality bonus, tier-scaled AI, and reward formulas. The live game uses the
    /// circuit races in CircuitRaceSystem; this single-shot model is kept for reference and reuse.</summary>
    public class RaceSystem
    {
        public const float TrackMeters = 402f;
        public const float CooldownSeconds = 180f;

        private readonly GameConfig _cfg;
        private readonly GameState _state;
        private readonly GameEventBus _bus;
        private readonly Random _rng;

        public RaceSystem(GameConfig cfg, GameState state, GameEventBus bus, Random rng)
        { _cfg = cfg; _state = state; _bus = bus; _rng = rng ?? new Random(); }

        public bool CanRace => _state.raceCooldownRemaining <= 0f;

        public void Tick(float dt)
        {
            if (_state.raceCooldownRemaining > 0f)
                _state.raceCooldownRemaining = Math.Max(0f, _state.raceCooldownRemaining - dt);
        }

        public static float LaunchBonus(RaceLaunch launch) => launch switch
        {
            RaceLaunch.Perfect => 1.15f,
            RaceLaunch.Good => 1.05f,
            RaceLaunch.Bad => 0.85f,
            _ => 1f
        };

        private float AiSpeedMultiplier(int tier) => tier switch
        {
            1 => 0.70f, 2 => 0.85f, 3 => 0.95f, _ => 1.05f
        };

        /// <summary>Resolves a race using a graded car the player owns. Consumes nothing (the car
        /// races, it isn't destroyed).</summary>
        public RaceResultEvent Resolve(string carKey, RaceLaunch launch, bool perfectLaunch)
        {
            int tier = _state.factoryTierIndex + 1;
            CarKey.TryParse(carKey, out var vehicleId, out var grade);
            var v = _cfg.GetVehicle(vehicleId);
            var stats = v != null ? v.baseRaceStats : default;

            float playerPower = (stats.topSpeed + stats.acceleration + stats.traction + stats.launch + 1f)
                                * LaunchBonus(launch);
            float playerTime = TrackMeters / Math.Max(1f, playerPower);
            float opponentTime = playerTime / Math.Max(0.5f, AiSpeedMultiplier(tier))
                                 * (0.9f + (float)_rng.NextDouble() * 0.2f);

            bool won = playerTime <= opponentTime;
            long gradedPrice = v != null ? (long)Math.Floor(v.basePrice * _cfg.gachaTable.GetMultiplier(grade)) : 0;

            long cash;
            int trophies;
            if (won)
            {
                cash = (long)Math.Floor(gradedPrice * (perfectLaunch ? 3.0 : 2.0));
                trophies = perfectLaunch ? 2 : 1;
            }
            else
            {
                cash = (long)Math.Floor(gradedPrice * 0.5);
                trophies = 0;
            }

            _state.wallet.AddCash(cash);
            if (trophies > 0) _state.wallet.AddTrophies(trophies);
            _state.raceCooldownRemaining = CooldownSeconds;
            _state.racesCompleted++;

            var result = new RaceResultEvent(won, cash, trophies, playerTime, opponentTime);
            _bus?.raceFinished?.Raise(result);
            _bus?.CashChanged(_state.wallet.cash);
            if (trophies > 0) _bus?.TrophiesChanged(_state.wallet.trophies);
            return result;
        }
    }
}
