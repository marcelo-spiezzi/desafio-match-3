using DG.Tweening;
using DG.Tweening.Core.Easing;
using Gazeus.DesafioMatch3.Views;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3
{
    public class ToggleButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform toggle;

        [Header("Settings")]
        [SerializeField] private float leftX = -32.0f;
        [SerializeField] private float rightX = 32.0f;

        [Header("Animation Settings")]
        [SerializeField] private float toggleDuration = 0.2f;
        [SerializeField] private Ease toggleEase;

        bool state = true;
        
        public void toggleState()
        {
            state = !state;
            ShowMainMenu();
        }

        private Tween ShowMainMenu()
        {
            float pos = state ? leftX : rightX;
            return DOVirtual.Float(toggle.anchoredPosition.x, pos, toggleDuration, (value) =>
            {
                toggle.anchoredPosition = new Vector2(value, toggle.anchoredPosition.y);
            }).SetEase(toggleEase);
        }

    }
}
