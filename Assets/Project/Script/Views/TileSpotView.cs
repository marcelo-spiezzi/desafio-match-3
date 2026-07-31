using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.Views
{
    public class TileSpotView : MonoBehaviour
    {
        public event Action<int, int> Clicked;

        [SerializeField] private Button _button;

        private int _x;
        private int _y;

        #region Unity
        private void Awake()
        {
            _button.onClick.AddListener(OnTileClick);
        }
        #endregion

        public Tween AnimatedSetTile(GameObject tile, bool isValidCombination, float moveDuration, Ease moveCurve, float wrongScale, float wrongDuration)
        {
            tile.transform.SetParent(transform);
            tile.transform.DOKill();

            if (!isValidCombination) tile.transform.DOPunchScale(Vector3.one * wrongScale, wrongDuration);

            return tile.transform.DOMove(transform.position, moveDuration).SetEase(moveCurve);
        }

        public void SetPosition(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public void SetTile(GameObject tile)
        {
            tile.transform.SetParent(transform, false);
            tile.transform.position = transform.position;
        }

        private void OnTileClick()
        {
            Clicked?.Invoke(_x, _y);
        }
    }
}
