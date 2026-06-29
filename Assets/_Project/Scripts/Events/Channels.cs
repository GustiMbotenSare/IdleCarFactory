using UnityEngine;

namespace CarFactoryIdle.Events
{
    [CreateAssetMenu(menuName = "CFI/Events/Long Channel", fileName = "LongChannel")]
    public class LongEventChannel : EventChannel<long> { }

    [CreateAssetMenu(menuName = "CFI/Events/String Channel", fileName = "StringChannel")]
    public class StringEventChannel : EventChannel<string> { }

    [CreateAssetMenu(menuName = "CFI/Events/Void Channel", fileName = "VoidChannel")]
    public class VoidEventChannel : EventChannel { }

    [CreateAssetMenu(menuName = "CFI/Events/Car Assembled Channel", fileName = "CarAssembledChannel")]
    public class CarAssembledEventChannel : EventChannel<CarAssembledEvent> { }

    [CreateAssetMenu(menuName = "CFI/Events/Car Sold Channel", fileName = "CarSoldChannel")]
    public class CarSoldEventChannel : EventChannel<CarSoldEvent> { }

    [CreateAssetMenu(menuName = "CFI/Events/Race Result Channel", fileName = "RaceResultChannel")]
    public class RaceResultEventChannel : EventChannel<RaceResultEvent> { }

    [CreateAssetMenu(menuName = "CFI/Events/Toast Channel", fileName = "ToastChannel")]
    public class ToastEventChannel : EventChannel<ToastEvent> { }

    /// <summary>Bundle of channels the simulation raises. Optional: any field may be null
    /// (e.g. during headless tests). Wire real assets via GameRoot in the editor.</summary>
    [CreateAssetMenu(menuName = "CFI/Events/Game Event Bus", fileName = "GameEventBus")]
    public class GameEventBus : ScriptableObject
    {
        public LongEventChannel cashChanged;
        public LongEventChannel trophiesChanged;
        public CarAssembledEventChannel carAssembled;
        public CarSoldEventChannel carSold;
        public RaceResultEventChannel raceFinished;
        public StringEventChannel error;            // "Not enough cash!"
        public StringEventChannel milestone;        // for reactive mascot
        public VoidEventChannel contractsRefreshed;
        public VoidEventChannel stateChanged;       // coarse "redraw" signal
        public ToastEventChannel toastRequested;    // transient on-screen feedback

        public void CashChanged(long v) => cashChanged?.Raise(v);
        public void TrophiesChanged(long v) => trophiesChanged?.Raise(v);

        /// <summary>Errors double as error toasts so every failed intent surfaces to the player.</summary>
        public void Error(string m) { error?.Raise(m); toastRequested?.Raise(new ToastEvent(m, ToastKind.Error)); }
        public void Milestone(string m) => milestone?.Raise(m);

        /// <summary>Request a transient toast directly (info/success/error).</summary>
        public void Toast(string m, ToastKind kind) => toastRequested?.Raise(new ToastEvent(m, kind));

        /// <summary>A car sale: raises the typed channel and a success toast.</summary>
        public void CarSold(CarSoldEvent e)
        {
            carSold?.Raise(e);
            string msg = e.ViaAuction
                ? $"Auction won \u2014 sold for {CarFactoryIdle.Core.NumberFormat.Currency(e.Amount)}"
                : $"Car sold for {CarFactoryIdle.Core.NumberFormat.Currency(e.Amount)}";
            toastRequested?.Raise(new ToastEvent(msg, ToastKind.Success));
        }
    }
}
