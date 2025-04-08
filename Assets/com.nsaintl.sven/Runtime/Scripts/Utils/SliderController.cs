using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sven.Utils
{
    [RequireComponent(typeof(Slider))]
    public class SliderController : MonoBehaviour
    {
        [BoxGroup("View")] public TextMeshProUGUI valueText;

        public void Awake()
        {
            Slider slider = GetComponent<Slider>();
            slider.onValueChanged.AddListener(OnValueChanged);
            valueText.text = slider.value.ToString();
        }

        private void OnValueChanged(float arg0)
        {
            valueText.text = arg0.ToString();
        }
    }
}