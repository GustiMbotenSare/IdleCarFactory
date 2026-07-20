using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CarFactoryIdle.Data;
using CarFactoryIdle.Core;
using CarFactoryIdle.Events;

namespace CarFactoryIdle.UI
{
    /// <summary>
    /// Top-level controller for the Factory/Production screen.
    /// Builds the station list and vehicle picker from GameFacade.Config,
    /// then refreshes every UI element whenever the game state changes.
    /// </summary>
    public class FactoryScreen : MonoBehaviour
    {
        [Header("Station List")]
        [SerializeField] private Transform stationListContent;
        [SerializeField] private StationRowView stationRowPrefab;
        [SerializeField] private CategoryHeaderView categoryHeaderPrefab;

        [Header("Vehicle Picker (Optional)")]
        [SerializeField] private Transform vehicleListContent;
        [SerializeField] private VehicleOptionView vehicleOptionPrefab;
        [SerializeField] private Button buildButton;

        [Header("Factory Tier")]
        [SerializeField] private TMP_Text factoryTierText;
        [SerializeField] private TMP_Text productionBonusText;
        [SerializeField] private Button factoryUpgradeButton;
        [SerializeField] private TMP_Text factoryUpgradeButtonText;

        [Header("Events")]
        [SerializeField] private VoidEventChannel stateChanged;

        private readonly List<StationRowView> _stationRows = new();
        private readonly List<VehicleOptionView> _vehicleOptions = new();

        private void Start()
        {
            Debug.Log("========== FactoryScreen Start ==========");
            Debug.Log("FactoryScreen Object : " + gameObject.name);
            Debug.Log("FactoryScreen InstanceID : " + GetInstanceID());

            BuildStationList();

            if (vehicleListContent != null && vehicleOptionPrefab != null)
                BuildVehicleList();

            if (buildButton != null)
            {
                buildButton.onClick.RemoveAllListeners();
                buildButton.onClick.AddListener(() =>
                {
                    Debug.Log("Build Button Clicked");
                    GameServices.Facade.BuildSelectedOnce();
                });
            }

            if (factoryUpgradeButton == null)
            {
                Debug.LogError("Factory Upgrade Button NULL");
            }
            else
            {
                Debug.Log("Factory Upgrade Button = " + factoryUpgradeButton.name);
                Debug.Log("Button InstanceID = " + factoryUpgradeButton.GetInstanceID());

                factoryUpgradeButton.onClick.RemoveAllListeners();

                factoryUpgradeButton.onClick.AddListener(() =>
                {
                    Debug.Log("===== LAMBDA CLICK =====");
                });

                factoryUpgradeButton.onClick.AddListener(OnFactoryUpgradeClicked);

                Debug.Log("Factory listener registered.");
            }

            RefreshAll();
        }

        private void OnEnable()
        {
            if (stateChanged != null)
                stateChanged.Subscribe(RefreshAll);
        }

        private void OnDisable()
        {
            if (stateChanged != null)
                stateChanged.Unsubscribe(RefreshAll);
        }

        public void OnFactoryUpgradeClicked()
        {
            Debug.Log("===== OnFactoryUpgradeClicked =====");

            GameServices.Facade.BuyNextFactoryTier();
            RefreshFactory();
        }

        private void RefreshFactory()
        {
            var facade = GameServices.Facade;

            var currentTier =
                facade.Config.GetFactoryTier(facade.State.factoryTierIndex);

            var nextTier = facade.NextFactoryTier();

            Debug.Log(nextTier == null ? "No Next Tier" : "Next Tier Found");

            if (factoryTierText != null)
                factoryTierText.text = currentTier.displayName;

            if (productionBonusText != null)
            {
                productionBonusText.text =
                    $"Production x{facade.Config.ProductionMultiplier(facade.State.factoryTierIndex):0.00}";
            }

            if (factoryUpgradeButton == null || factoryUpgradeButtonText == null)
                return;

            if (nextTier == null)
            {
                factoryUpgradeButton.interactable = false;
                factoryUpgradeButtonText.text = "MAX";
            }
            else
            {
                factoryUpgradeButton.interactable = true;
                factoryUpgradeButtonText.text =
                    $"Upgrade {NumberFormat.Currency(nextTier.cost)}";
            }
            
        }

        private void BuildStationList()
        {
            Debug.Log("===== BuildStationList Dipanggil =====");

            var cfg = GameServices.Facade.Config;

            Debug.Log("Station Count = " + cfg.stations.Count);

            StationCategory[] order =
            {
                StationCategory.Extractor
            };

            string[] labels =
            {
                "Extractors"
            };

            for (int c = 0; c < order.Length; c++)
            {
                var group = cfg.stations.FindAll(s => s.category == order[c]);

                Debug.Log($"Category {order[c]} : {group.Count}");

                if (group.Count == 0)
                    continue;

                foreach (var def in group)
                {
                    Debug.Log("Spawn : " + def.displayName);

                    var row = Instantiate(stationRowPrefab, stationListContent);
                    row.Bind(def);
                    _stationRows.Add(row);
                }
            }
        }

        private void BuildVehicleList()
        {
            var cfg = GameServices.Facade.Config;

            foreach (var vehicle in cfg.vehicles)
            {
                var option =
                    Instantiate(vehicleOptionPrefab, vehicleListContent);

                option.Bind(vehicle);
                _vehicleOptions.Add(option);
            }
        }

        private void RefreshAll()
        {
            foreach (var row in _stationRows)
                row.Refresh();

            foreach (var option in _vehicleOptions)
                option.Refresh();

            RefreshFactory();
        }
    }
}