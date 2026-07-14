using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CarFactoryIdle.Data;
using CarFactoryIdle.Core;

namespace CarFactoryIdle.UI
{
    /// <summary>One selectable entry in the Factory screen's vehicle picker.</summary>
    public class VehicleOptionView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedMarker;

        private string _vehicleId;

        public void Bind(VehicleDefinition def)
        {
            _vehicleId = def.id;
            nameText.text = $"T{def.tier} {def.displayName}";
            priceText.text = NumberFormat.Currency(def.basePrice);

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => GameServices.Facade.SelectVehicle(_vehicleId));

            Refresh();
        }

        public void Refresh()
        {
            bool selected = GameServices.Facade.State.selectedVehicleId == _vehicleId;
            if (selectedMarker != null) selectedMarker.SetActive(selected);
        }
    }
}
