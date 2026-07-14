using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CarFactoryIdle.Data;
using CarFactoryIdle.Core;

namespace CarFactoryIdle.UI
{
    /// <summary>One row in the Factory screen's station list. Pure view: reads GameFacade.State/Config
    /// for display and forwards button clicks to GameFacade. No game rules live here.
    ///
    /// Output Count Text is optional — leave it unassigned if your prefab doesn't have it yet.</summary>
    public class StationRowView : MonoBehaviour
    {
        [Header("Info")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private TMP_Text outputCountText;
        [SerializeField] private Image progressFill;

        [Header("Actions (each button's own child TMP text doubles as its cost label)")]
        [SerializeField] private Button tapButton;
        [SerializeField] private Button unlockButton;
        [SerializeField] private TMP_Text unlockCostText;
        [SerializeField] private Button automateButton;
        [SerializeField] private TMP_Text automateCostText;
        [SerializeField] private Button speedButton;
        [SerializeField] private TMP_Text speedCostText;
        [SerializeField] private Button capacityButton;
        [SerializeField] private TMP_Text capacityCostText;

        private string _stationId;
        private StationDefinition _def;

        public void Bind(StationDefinition def)
        {
            _def = def;
            _stationId = def.id;
            nameText.text = def.displayName;

            tapButton.onClick.RemoveAllListeners();
            tapButton.onClick.AddListener(() => GameServices.Facade.TapStation(_stationId));
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(() => GameServices.Facade.UnlockStation(_stationId));
            automateButton.onClick.RemoveAllListeners();
            automateButton.onClick.AddListener(() => GameServices.Facade.BuyAutomation(_stationId));
            speedButton.onClick.RemoveAllListeners();
            speedButton.onClick.AddListener(() => GameServices.Facade.UpgradeSpeed(_stationId));
            capacityButton.onClick.RemoveAllListeners();
            capacityButton.onClick.AddListener(() => GameServices.Facade.UpgradeCapacity(_stationId));

            Refresh();
        }

        public void Refresh()
        {
            if (_def == null) return;
            var facade = GameServices.Facade;
            var ss = facade.State.GetStation(_stationId);
            if (ss == null) return;

            bool canManualTap = _def.category == StationCategory.Extractor || _def.category == StationCategory.Manufacturing;

            if (outputCountText != null && !string.IsNullOrEmpty(_def.outputItemId))
                outputCountText.text = NumberFormat.Format(facade.State.inventory.Get(_def.outputItemId));

            if (!ss.unlocked)
            {
                stateText.text = "Locked";
                speedText.text = "-";
                capacityText.text = "-";
                if (progressFill != null) progressFill.fillAmount = 0f;

                tapButton.gameObject.SetActive(false);
                unlockButton.gameObject.SetActive(true);
                unlockCostText.text = NumberFormat.Currency(_def.unlockCost);
                automateButton.gameObject.SetActive(false);
                speedButton.gameObject.SetActive(false);
                capacityButton.gameObject.SetActive(false);
                return;
            }

            unlockButton.gameObject.SetActive(false);
            stateText.text = ss.automated ? "Automated" : "Manual";
            speedText.text = $"Speed Lv.{ss.speedLevel}/{Economy.MaxSpeedLevel}";
            capacityText.text = $"Capacity Lv.{ss.capacityLevel}/{Economy.MaxCapacityLevel}";

            tapButton.gameObject.SetActive(canManualTap);
            if (canManualTap)
                tapButton.interactable = _def.category == StationCategory.Extractor || !ss.automated;

            automateButton.gameObject.SetActive(!ss.automated);
            if (!ss.automated) automateCostText.text = NumberFormat.Currency(_def.automationCost);

            bool speedMaxed = ss.speedLevel >= Economy.MaxSpeedLevel;
            speedButton.gameObject.SetActive(!speedMaxed);
            if (!speedMaxed) speedCostText.text = NumberFormat.Currency(facade.SpeedUpgradeCost(_stationId));

            bool capMaxed = ss.capacityLevel >= Economy.MaxCapacityLevel;
            capacityButton.gameObject.SetActive(!capMaxed);
            if (!capMaxed) capacityCostText.text = NumberFormat.Currency(facade.CapacityUpgradeCost(_stationId));

            if (progressFill != null)
            {
                float fmult = facade.Config.ProductionMultiplier(facade.State.factoryTierIndex);
                float interval = Economy.EffectiveInterval(_def.baseIntervalSeconds, ss.speedLevel, fmult);
                progressFill.fillAmount = interval > 0f ? Mathf.Clamp01(ss.progress / interval) : 0f;
            }
        }
    }
}
