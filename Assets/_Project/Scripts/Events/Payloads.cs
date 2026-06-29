using CarFactoryIdle.Data;

namespace CarFactoryIdle.Events
{
    public readonly struct CarAssembledEvent
    {
        public readonly string VehicleId;
        public readonly Grade Grade;
        public readonly string Key;
        public CarAssembledEvent(string vehicleId, Grade grade, string key)
        { VehicleId = vehicleId; Grade = grade; Key = key; }
    }

    public readonly struct CarSoldEvent
    {
        public readonly string Key;
        public readonly long Amount;
        public readonly bool ViaAuction;
        public CarSoldEvent(string key, long amount, bool viaAuction)
        { Key = key; Amount = amount; ViaAuction = viaAuction; }
    }

    public enum RaceLaunch { Bad, Good, Perfect }

    /// <summary>Severity/visual style for a transient toast notification.</summary>
    public enum ToastKind { Info, Success, Error }

    /// <summary>A request to show a transient toast. Display-only; carries no game state.</summary>
    public readonly struct ToastEvent
    {
        public readonly string Message;
        public readonly ToastKind Kind;
        public ToastEvent(string message, ToastKind kind) { Message = message; Kind = kind; }
    }

    public readonly struct RaceResultEvent
    {
        public readonly bool Won;
        public readonly long CashReward;
        public readonly int TrophyReward;
        public readonly float PlayerTime;
        public readonly float OpponentTime;
        public RaceResultEvent(bool won, long cash, int trophies, float playerTime, float opponentTime)
        { Won = won; CashReward = cash; TrophyReward = trophies; PlayerTime = playerTime; OpponentTime = opponentTime; }
    }
}
