using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CarFactoryIdle.Data;
using CarFactoryIdle.State;

namespace CarFactoryIdle.UI
{
    public class StationCardUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text outputText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button actionButton;

        private StationDefinition definition;
        private StationState state;

        public StationDefinition Definition => definition;
        public StationState State => state;

        public void Initialize(StationDefinition definition, StationState state)
        {
            this.definition = definition;
            this.state = state;

            Refresh();

            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionPressed);
        }

        public void Refresh()
        {
            if (definition == null || state == null)
                return;

            nameText.text = definition.displayName;

            outputText.text = $"Output : {definition.baseOutput}";

            if (progressBar != null)
            {
                progressBar.maxValue = definition.baseIntervalSeconds;
                progressBar.value = state.progress;
            }

            actionButton.interactable = state.unlocked;
        }

        private void OnActionPressed()
        {
            Debug.Log($"Pressed {definition.displayName}");

            // Nanti kita hubungkan ke GameFacade
            // Contoh:
            // GameRoot.Instance.Facade.StartProduction(definition.id);
        }
    }
}