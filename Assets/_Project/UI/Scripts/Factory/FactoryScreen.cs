using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CarFactoryIdle.Data;
using CarFactoryIdle.Core;
using CarFactoryIdle.Events;

namespace CarFactoryIdle.UI
{
    /// <summary>Top-level controller for the Factory/Production screen: builds the station list and
    /// vehicle picker once from GameFacade.Config, then refreshes every row on the stateChanged event.
    /// No game rules live here — every action forwards straight to GameFacade.
    ///
    /// Vehicle picker and Factory tier fields are optional — leave them unassigned if your scene
    /// isn't wired up to them yet (e.g. while working on the extractor-only slice first).</summary>
    public class FactoryScreen : MonoBehaviour
    {
        [Header("Station list")]
        [SerializeField] private Transform stationListContent;
        [SerializeField] private StationRowView stationRowPrefab;
        [SerializeField] private CategoryHeaderView categoryHeaderPrefab;

        [Header("Vehicle picker (optional)")]
        [SerializeField] private Transform vehicleListContent;
        [SerializeField] private VehicleOptionView vehicleOptionPrefab;
        [SerializeField] private Button buildButton;

        [Header("Factory tier (optional)")]
        [SerializeField] private TMP_Text tierNameText;
        [SerializeField] private Button nextTierButton;
        [SerializeField] private TMP_Text nextTierCostText;

        [Header("Events")]
        [SerializeField] private VoidEventChannel stateChanged;

        private readonly List<StationRowView> _stationRows = new();
        private readonly List<VehicleOptionView> _vehicleOptions = new();

        private void Start()
        {
            BuildStationList();
            if (vehicleListContent != null && vehicleOptionPrefab != null) BuildVehicleList();

            if (buildButton != null)
                buildButton.onClick.AddListener(() => GameServices.Facade.BuildSelectedOnce());
            if (nextTierButton != null)
                nextTierButton.onClick.AddListener(() => GameServices.Facade.BuyNextFactoryTier());

            RefreshAll();
        }

        private void OnEnable()
        {
            if (stateChanged != null) stateChanged.Subscribe(RefreshAll);
        }

        private void OnDisable()
        {
            if (stateChanged != null) stateChanged.Unsubscribe(RefreshAll);
        }

        private void BuildStationList()
        {
            var cfg = GameServices.Facade.Config;
            // Filtered to Extractor only for the current build phase. To bring the other categories
            // back later, restore: { Extractor, Manufacturing, Assembly, Sales } / { "Extractors", ... }.
            StationCategory[] order = { StationCategory.Extractor };
            string[] labels = { "Extractors" };

            for (int c = 0; c < order.Length; c++)
            {
                var group = cfg.stations.FindAll(s => s.category == order[c]);
                if (group.Count == 0) continue;

                if (categoryHeaderPrefab != null)
                {
                    var header = Instantiate(categoryHeaderPrefab, stationListContent);
                    header.SetLabel(labels[c]);
                }

                foreach (var def in group)
                {
                    var row = Instantiate(stationRowPrefab, stationListContent);
                    row.Bind(def);
                    _stationRows.Add(row);
                }
            }
        }

        private void BuildVehicleList()
        {
            var cfg = GameServices.Facade.Config;
            foreach (var v in cfg.vehicles)
            {
                var opt = Instantiate(vehicleOptionPrefab, vehicleListContent);
                opt.Bind(v);
                _vehicleOptions.Add(opt);
            }
        }

        private void RefreshAll()
        {
            foreach (var row in _stationRows) row.Refresh();
            foreach (var opt in _vehicleOptions) opt.Refresh();

            var facade = GameServices.Facade;

            if (tierNameText != null)
            {
                var tier = facade.Config.GetFactoryTier(facade.State.factoryTierIndex);
                tierNameText.text = tier != null ? $"Tier {tier.tier}: {tier.displayName}" : "-";
            }

            if (nextTierButton != null)
            {
                var next = facade.NextFactoryTier();
                nextTierButton.gameObject.SetActive(next != null);
                if (next != null && nextTierCostText != null) nextTierCostText.text = NumberFormat.Currency(next.cost);
            }
        }
    }
}
