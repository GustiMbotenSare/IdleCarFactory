using UnityEngine;
using TMPro;
using CarFactoryIdle.Core;
using CarFactoryIdle.Events;

namespace CarFactoryIdle.UI
{
    /// <summary>Persistent cash/trophy readout. Pure view: initializes from GameFacade.State once,
    /// then stays in sync by listening to the wallet event channels.</summary>
    public class WalletHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text cashText;
        [SerializeField] private TMP_Text trophiesText;
        [SerializeField] private LongEventChannel cashChanged;
        [SerializeField] private LongEventChannel trophiesChanged;

        private void Start()
        {
            var wallet = GameServices.Facade.State.wallet;
            SetCash(wallet.cash);
            SetTrophies(wallet.trophies);
        }

        private void OnEnable()
        {
            if (cashChanged != null) cashChanged.Subscribe(SetCash);
            if (trophiesChanged != null) trophiesChanged.Subscribe(SetTrophies);
        }

        private void OnDisable()
        {
            if (cashChanged != null) cashChanged.Unsubscribe(SetCash);
            if (trophiesChanged != null) trophiesChanged.Unsubscribe(SetTrophies);
        }

        private void SetCash(long v) => cashText.text = NumberFormat.Currency(v);
        private void SetTrophies(long v) => trophiesText.text = "Trophies: " + NumberFormat.Format(v);
    }
}
