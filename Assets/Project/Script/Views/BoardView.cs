using DG.Tweening;
using Gazeus.DesafioMatch3.Controllers;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.ScriptableObjects;
using System;
using System.Collections.Generic;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.ParticleSystem;

namespace Gazeus.DesafioMatch3.Views
{
    public class BoardView : MonoBehaviour
    {
        public event Action<int, int> TileClicked;

        [SerializeField] private GridLayoutGroup _boardContainer;
        [SerializeField] private TilePrefabRepository _tilePrefabRepository;
        [SerializeField] private TileSpotView _tileSpotPrefab;

        private GameObject[][] _tiles;
        private TileSpotView[][] _tileSpots;
        private float particleScaler = 1.0f;

        public void AdjustCellSize(int width, int height, int maxSize, int minSize, int padding)
        {
            int cellSize = Mathf.Min(Mathf.Clamp((Screen.width - padding) / width, minSize, maxSize), Mathf.Clamp((Screen.height - padding) / height, minSize, maxSize));
            _boardContainer.cellSize = new Vector2(cellSize, cellSize);
            particleScaler = (float) cellSize / minSize;
        }

        //This will break game tweens, there is no OnDestroy checks on the game logic ATM
        public void DestroyBoard(float blendDuration, Ease boardOutCurve)
        {
            _boardContainer.GetComponent<CanvasGroup>().interactable = false;
            _boardContainer.transform.DOScale(0.0f, blendDuration).OnComplete(() =>
            {
                for (int i = _boardContainer.transform.childCount - 1; i >= 0; i--)
                {
                    GameObject.Destroy(_boardContainer.transform.GetChild(i).gameObject);
                }
            }).SetEase(boardOutCurve);
        }

        public void CreateBoard(List<List<Tile>> board, float blendDuration, Ease boardInCurve)
        {
            _boardContainer.transform.localScale = Vector3.one;
            _boardContainer.GetComponent<CanvasGroup>().interactable = false;

            _boardContainer.constraintCount = board[0].Count;
            _tiles = new GameObject[board.Count][];
            _tileSpots = new TileSpotView[board.Count][];

            for (int y = 0; y < board.Count; y++)
            {
                _tiles[y] = new GameObject[board[0].Count];
                _tileSpots[y] = new TileSpotView[board[0].Count];

                for (int x = 0; x < board[0].Count; x++)
                {
                    TileSpotView tileSpot = Instantiate(_tileSpotPrefab);
                    tileSpot.transform.SetParent(_boardContainer.transform, false);
                    tileSpot.SetPosition(x, y);
                    tileSpot.Clicked += TileSpot_Clicked;

                    _tileSpots[y][x] = tileSpot;

                    int tileTypeIndex = board[y][x].Type;
                    if (tileTypeIndex > -1)
                    {
                        GameObject tilePrefab = _tilePrefabRepository.TileTypePrefabList[tileTypeIndex];
                        GameObject tile = Instantiate(tilePrefab);
                        tileSpot.SetTile(tile);

                        _tiles[y][x] = tile;
                    }
                }
            }

            _boardContainer.transform.localScale = Vector3.zero;
            _boardContainer.transform.DOScale(1.0f, blendDuration).OnComplete(() =>
            {
                _boardContainer.GetComponent<CanvasGroup>().interactable = true;
            }).SetEase(boardInCurve);
        }

        public Tween CreateTile(List<AddedTileInfo> addedTiles)
        {
            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < addedTiles.Count; i++)
            {
                AddedTileInfo addedTileInfo = addedTiles[i];
                Vector2Int position = addedTileInfo.Position;

                TileSpotView tileSpot = _tileSpots[position.y][position.x];

                GameObject tilePrefab = _tilePrefabRepository.TileTypePrefabList[addedTileInfo.Type];
                GameObject tile = Instantiate(tilePrefab);
                tileSpot.SetTile(tile);

                _tiles[position.y][position.x] = tile;

                tile.transform.localScale = Vector2.zero;
                sequence.Join(tile.transform.DOScale(1.0f, 0.2f));
            }

            return sequence;
        }

        private void SpawnParticle(GameObject tile, ParticleSystem particle, float delay, Color tileColor)
        {
            Vector3 pos = new Vector3(tile.transform.position.x, tile.transform.position.y, 0.0f);
            var p = Instantiate(particle.gameObject, pos, Quaternion.identity);
            p.transform.localScale = p.transform.localScale * particleScaler;

            ParticleSystem ps = p.GetComponent<ParticleSystem>();
            var mainModule = ps.main;

            mainModule.startDelay = delay;
            mainModule.startColor = tileColor;
        }

        public Tween ComboEffects(List<Vector2Int> matchedPosition, float dissolveScale, float dissolveDuration, Ease dissolveCurve, Ease dissolveShaderCurve, 
            Vector3 comboRotation, Ease comboRotateCurve, ParticleSystem particle, float particleDelayPerc, float bounceBGDuration, Ease bounceBGCurve, 
            float shineBGDuration, Ease shineBGCurve)
        {
            int combinations = matchedPosition.Count;

            for (int i = 0; i < combinations; i++)
            {
                Vector2Int position = matchedPosition[i];
                GameObject tile = _tiles[position.y][position.x];

                UnityEngine.UI.Image img = tile.GetComponent<UnityEngine.UI.Image>();
                Material instanceMat = Instantiate(img.material);
                img.material = instanceMat;

                tile.transform.DOScale(Vector3.one * dissolveScale, dissolveDuration).SetEase(dissolveCurve);

                if (combinations > 3)
                {
                    instanceMat.DOFloat(0.0f, "_Dissolve", dissolveDuration).SetEase(dissolveShaderCurve);
                    tile.transform.DORotate(comboRotation, dissolveDuration).SetEase(comboRotateCurve);
                }
                else
                {
                    instanceMat.DOFloat(0.0f, "_RegularDissolve", dissolveDuration).SetEase(Ease.Linear);
                }

                DOVirtual.Float(0f, 1f, bounceBGDuration, (value) => {
                    Shader.SetGlobalFloat("_BounceBG", value);
                }).SetEase(bounceBGCurve).SetLoops(2, LoopType.Yoyo);

                var color = tile.GetComponentInChildren<UnityEngine.UI.Image>().color;
                SpawnParticle(_tiles[position.y][position.x], particle, dissolveDuration * particleDelayPerc, color);
            }

            return DOVirtual.DelayedCall(dissolveDuration, () => { });
        }

        public Tween DestroyTiles(List<Vector2Int> matchedPosition)
        {
            for (int i = 0; i < matchedPosition.Count; i++)
            {
                Vector2Int position = matchedPosition[i];
                Destroy(_tiles[position.y][position.x]);
                _tiles[position.y][position.x] = null;
            }

            return DOVirtual.DelayedCall(0.05f, () => { });
        }

        public Tween MoveTiles(List<MovedTileInfo> movedTiles, float moveDuration, Ease moveCurve, float wrongScale, float wrongDuration)
        {
            GameObject[][] tiles = new GameObject[_tiles.Length][];
            for (int y = 0; y < _tiles.Length; y++)
            {
                tiles[y] = new GameObject[_tiles[y].Length];
                for (int x = 0; x < _tiles[y].Length; x++)
                {
                    tiles[y][x] = _tiles[y][x];
                }
            }

            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < movedTiles.Count; i++)
            {
                MovedTileInfo movedTileInfo = movedTiles[i];

                Vector2Int from = movedTileInfo.From;
                Vector2Int to = movedTileInfo.To;

                sequence.Join(_tileSpots[to.y][to.x].AnimatedSetTile(_tiles[from.y][from.x], true, moveDuration, moveCurve, wrongScale, wrongDuration));

                tiles[to.y][to.x] = _tiles[from.y][from.x];
            }

            _tiles = tiles;

            return sequence;
        }

        public Tween SwapTiles(int fromX, int fromY, int toX, int toY, bool validMove, float moveDuration, Ease moveCurve, float wrongScale, float wrongDuration)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(_tileSpots[fromY][fromX].AnimatedSetTile(_tiles[toY][toX], validMove, moveDuration, moveCurve, wrongScale, wrongDuration));
            sequence.Join(_tileSpots[toY][toX].AnimatedSetTile(_tiles[fromY][fromX], validMove, moveDuration, moveCurve, wrongScale, wrongDuration));

            (_tiles[toY][toX], _tiles[fromY][fromX]) = (_tiles[fromY][fromX], _tiles[toY][toX]);

            return sequence;
        }

        #region Events
        private void TileSpot_Clicked(int x, int y)
        {
            TileClicked(x, y);
        }
        #endregion
    }
}
