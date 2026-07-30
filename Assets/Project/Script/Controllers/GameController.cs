using DG.Tweening;
using Gazeus.DesafioMatch3.Core;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

namespace Gazeus.DesafioMatch3.Controllers
{
    public class GameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardView _boardView;
        [SerializeField] private CanvasGroup mainMenu;
        [SerializeField] private RectTransform mainMenuPanel;
        [SerializeField] private RectTransform mainMenuAnchorRef;
        [SerializeField] private CanvasGroup gameMenu;

        [Header("Menu Animation Settings")]
        [SerializeField] private float moveX = 300f;
        [SerializeField] private float showMenuDuration = 1.2f;
        [SerializeField] private float hideMenuDuration = 1.2f;
        [SerializeField] private float boardInDuration = 0.8f;
        [SerializeField] private float boardOutDuration = 0.4f;

        [Header("Board Config")]
        [SerializeField] private int _boardHeight = 10;
        [SerializeField] private int _boardWidth = 10;
        [SerializeField] private int _maxCellSize = 80;

        [Header("Game Animation Settings")]
        [SerializeField] private float dissolveScale = 1.2f;
        [SerializeField] private float dissolveDuration = 0.1f;
        [SerializeField] private Vector3 comboRotation;
        [SerializeField] private ParticleSystem destroyParticle;

        private GameService _gameService;
        private bool _isAnimating;
        private int _selectedX = -1;
        private int _selectedY = -1;
        private bool _isGameActive = false;

        #region Unity
        private void Awake()
        {
            _gameService = new GameService();
            _boardView.TileClicked += OnTileClick;
        }

        private void OnDestroy()
        {
            _boardView.TileClicked -= OnTileClick;
        }

        private void Start()
        {
            _isGameActive = false;
            gameMenu.gameObject.SetActive(false);
            mainMenuPanel.transform.position = new Vector3(mainMenuAnchorRef.transform.position.x + moveX, 
                mainMenuPanel.transform.position.y, mainMenuPanel.transform.position.z);
            ShowMainMenu();

        }
        #endregion
        public void NewGame(int size)
        {
            if (_isGameActive) return;

            _boardHeight = size;
            _boardWidth = size;
            
            _boardView.gameObject.SetActive(true);
            _isGameActive = true;
            HideMainMenu();
            ShowGameMenu();

            List<List<Tile>> board = _gameService.StartGame(_boardWidth, _boardHeight);
            _boardView.CreateBoard(board, boardInDuration);
            _boardView.AdjustCellSize(_boardWidth, _boardHeight, _maxCellSize);
        }

        public void ExitGame()
        {
            _isGameActive = false;
            _boardView.DestroyBoard(boardOutDuration); //not performing any checks in here, assuming tweens and other code would do OnDestroy() validations at a final product scenario
            ShowMainMenu();
            HideGameMenu();
        }

        private Tween HideMainMenu()
        {
            mainMenuPanel.DOMoveX(mainMenuAnchorRef.transform.position.x + moveX, hideMenuDuration);

            DOVirtual.Float(1.0f, 0.0f, hideMenuDuration, (value) =>
            {
                mainMenu.alpha = value;
            });

            return DOVirtual.DelayedCall(hideMenuDuration, () => {
                mainMenu.gameObject.SetActive(false);
            });
        }

        private Tween ShowMainMenu()
        {
            mainMenu.gameObject.SetActive(true);
            mainMenuPanel.DOMoveX(mainMenuAnchorRef.transform.position.x, showMenuDuration);

            DOVirtual.Float(0.0f, 1.0f, showMenuDuration*1.5f, (value) =>
            {
                mainMenu.alpha = value;
            });

            return DOVirtual.DelayedCall(showMenuDuration, () => { });
        }

        private void HideGameMenu()
        {
            gameMenu.gameObject.SetActive(false);
        }

        private void ShowGameMenu()
        {
            gameMenu.gameObject.SetActive(true);
        }

        private void AnimateBoard(List<BoardSequence> boardSequences, int index, Action onComplete)
        {
            BoardSequence boardSequence = boardSequences[index];

            _boardView.ComboEffects(boardSequence.MatchedPosition, dissolveScale, dissolveDuration, comboRotation).onComplete += () =>
            {
                Sequence sequence = DOTween.Sequence();
                sequence.Append(_boardView.DestroyTiles(boardSequence.MatchedPosition, destroyParticle));
                sequence.Append(_boardView.MoveTiles(boardSequence.MovedTiles));
                sequence.Append(_boardView.CreateTile(boardSequence.AddedTiles));

                index += 1;
                if (index < boardSequences.Count)
                {
                    sequence.onComplete += () => AnimateBoard(boardSequences, index, onComplete);
                }
                else
                {
                    sequence.onComplete += () => onComplete();
                }
            };
        }

        private void OnTileClick(int x, int y)
        {
            if (_isAnimating) return;

            if (_selectedX > -1 && _selectedY > -1)
            {
                if (Mathf.Abs(_selectedX - x) + Mathf.Abs(_selectedY - y) > 1)
                {
                    _selectedX = -1;
                    _selectedY = -1;
                }
                else
                {
                    _isAnimating = true;
                    _boardView.SwapTiles(_selectedX, _selectedY, x, y).onComplete += () =>
                    {
                        bool isValid = _gameService.IsValidMovement(_selectedX, _selectedY, x, y);
                        if (isValid)
                        {
                            List<BoardSequence> swapResult = _gameService.SwapTile(_selectedX, _selectedY, x, y);
                            AnimateBoard(swapResult, 0, () => _isAnimating = false);
                        }
                        else
                        {
                            _boardView.SwapTiles(x, y, _selectedX, _selectedY).onComplete += () => _isAnimating = false;
                        }
                        _selectedX = -1;
                        _selectedY = -1;
                    };
                }
            }
            else
            {
                _selectedX = x;
                _selectedY = y;
            }
        }
    }
}
