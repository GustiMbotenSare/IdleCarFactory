using UnityEngine;
using TMPro;

namespace CarFactoryIdle.UI
{
    /// <summary>A plain section label inserted before each station category group in the list.</summary>
    public class CategoryHeaderView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        public void SetLabel(string text) => label.text = text;
    }
}
