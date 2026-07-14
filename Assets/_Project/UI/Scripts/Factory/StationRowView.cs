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
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text outputCountText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionButtonText;

        private string _stationId;
        private StationDefinition _def;

        public void Bind(StationDefinition def)
        {
            _def = def;
            _stationId = def.id;
            nameText.text = def.displayName;

            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionClicked);

            Refresh();
        }

        private void OnActionClicked()
        {
            var facade = GameServices.Facade;
            var ss = facade.State.GetStation(_stationId);
            if (ss == null) return;

            if (!ss.unlocked) facade.UnlockStation(_stationId);
            else facade.TapStation(_stationId);
        }

        public void Refresh()
        {
            if (_def == null) return;
            var facade = GameServices.Facade;
            var ss = facade.State.GetStation(_stationId);
            if (ss == null) return;

            outputCountText.text = NumberFormat.Format(facade.State.inventory.Get(_def.outputItemId));
            actionButtonText.text = !ss.unlocked ? "Unlock " + NumberFormat.Currency(_def.unlockCost) : "Tap";
        }
    }
}
