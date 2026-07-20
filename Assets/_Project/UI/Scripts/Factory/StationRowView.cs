using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CarFactoryIdle.Data;
using CarFactoryIdle.Core;

namespace CarFactoryIdle.UI
{
    /// <summary>One material card: icon, name, current inventory count, and a single action button
    /// that reads "Unlock $cost" while locked, or "Tap" once unlocked. Pure view — forwards clicks to
    /// GameFacade and reads GameFacade.State for display, no game rules live here.</summary>
    public class StationRowView : MonoBehaviour
    {
        //[SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text outputCountText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button autoButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private Button capacityButton;
        [SerializeField] private TMP_Text actionButtonText;
        [SerializeField] private TMP_Text autoButtonText;
        [SerializeField] private TMP_Text speedButtonText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private Slider progressBar;

        private string _stationId;
        private StationDefinition _def;

        public void Bind(StationDefinition def)
        {
            _def = def;
            _stationId = def.id;
            nameText.text = def.displayName;

            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionClicked);

            autoButton.onClick.RemoveAllListeners();
            autoButton.onClick.AddListener(OnAutoClicked);

            speedButton.onClick.RemoveAllListeners();
            speedButton.onClick.AddListener(OnSpeedClicked);

            capacityButton.onClick.RemoveAllListeners();
            capacityButton.onClick.AddListener(OnCapacityClicked);

            Refresh();
        }

        private void OnActionClicked()
        {
            Debug.Log($"Tap button clicked : {_stationId}");
            var facade = GameServices.Facade;
            var ss = facade.State.GetStation(_stationId);
            if (ss == null) return;

            if (!ss.unlocked) facade.UnlockStation(_stationId);
            else facade.TapStation(_stationId);
            Debug.Log($"Inventory {_def.outputItemId} = {facade.State.inventory.Get(_def.outputItemId)}");
        }

        private void OnAutoClicked()
        {
            var facade = GameServices.Facade;
            Debug.Log($"Cash sebelum = {facade.State.wallet.cash}");
            bool success = facade.BuyAutomation(_stationId);

            Debug.Log($"Buy Automation {_stationId} = {success}");
            Debug.Log($"Cash sesudah = {facade.State.wallet.cash}");
            Refresh();
        }

        private void OnSpeedClicked()
        {
            var facade = GameServices.Facade;

            facade.UpgradeSpeed(_stationId);

            Refresh();
        }

        private void OnCapacityClicked()
        {
            GameServices.Facade.UpgradeCapacity(_stationId);
            Refresh();
        }

        private void Update()
        {
            if (_def == null)
                return;

            Refresh();
        }

        public void Refresh()
        {
            if (_def == null) return;

            var facade = GameServices.Facade;
            var ss = facade.State.GetStation(_stationId);
            if (ss == null) return;

            if (progressBar != null)
            {
                float interval = Economy.EffectiveInterval(
                    _def.baseIntervalSeconds,
                    ss.speedLevel,
                    facade.Config.ProductionMultiplier(facade.State.factoryTierIndex)
                );

                progressBar.maxValue = interval;
                progressBar.value = ss.progress;
            }

            if (autoButtonText != null)
            {
                autoButtonText.text = ss.automated
                    ? "AUTO ✓"
                    : $"AUTO {NumberFormat.Currency(_def.automationCost)}";
            }

            if (autoButton != null)
            {
                autoButton.interactable = !ss.automated;
            }

            speedButtonText.text =
                $"SPD Lv.{ss.speedLevel} ({NumberFormat.Currency(facade.SpeedUpgradeCost(_stationId))})";

            speedButton.interactable =
                ss.speedLevel < Economy.MaxSpeedLevel;

            outputCountText.text =
                NumberFormat.Format(facade.State.inventory.Get(_def.outputItemId));

            actionButtonText.text = !ss.unlocked ? "Unlock " + NumberFormat.Currency(_def.unlockCost) : "Tap";


            capacityText.text = $"CAP Lv.{ss.capacityLevel} ({NumberFormat.Currency(facade.CapacityUpgradeCost(_stationId))})";

            capacityButton.interactable = ss.capacityLevel < Economy.MaxCapacityLevel;
        }
    }
}
