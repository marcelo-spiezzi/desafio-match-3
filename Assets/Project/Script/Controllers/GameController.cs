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
using TMPro;

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
        [SerializeField] private Transform BoardTitle;


        [Header("Menu Animation Settings")]
        [SerializeField] private float moveMenuX = 300f;
        [SerializeField] private float showMenuDuration = 1.2f;
        [SerializeField] private Ease showMenuCurve;
        [SerializeField] private float hideMenuDuration = 1.2f;
        [SerializeField] private Ease hideMenuCurve;
        [SerializeField] private float showGameMenuDuration = 1.2f;
        [SerializeField] private float hideGameMenuDuration = 1.2f;
        [SerializeField] private float boardInDuration = 0.8f;
        [SerializeField] private Ease boardInCurve;
        [SerializeField] private float boardOutDuration = 0.4f;
        [SerializeField] private Ease boardOutCurve;


        [Header("Board Config")]
        [SerializeField] private int _maxCellSize = 80;
        [SerializeField] private int _minCellSize = 24;
        [SerializeField] private int _screenPadding = 160;

        [Header("Game Animation Settings")]
        [SerializeField] private float dissolveScale = 1.2f;
        [SerializeField] private float dissolveDuration = 0.1f;
        [SerializeField] private Vector3 comboRotation;
        [SerializeField] private ParticleSystem destroyParticle;

        private GameService _gameService;

        private int _boardHeight = 10;
        private int _boardWidth = 10;

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

            gameMenu.alpha = 0.0f;
            gameMenu.interactable = false;

            mainMenuPanel.transform.position = new Vector3(mainMenuAnchorRef.transform.position.x + moveMenuX, 
                mainMenuPanel.transform.position.y, mainMenuPanel.transform.position.z);
            ShowMainMenu();

        }

        public void UpdateBoardTitle(string name)
        {
            BoardTitle.GetComponent<TextMeshProUGUI>().text = name;
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
            _boardView.CreateBoard(board, boardInDuration, boardInCurve);
            _boardView.AdjustCellSize(_boardWidth, _boardHeight, _maxCellSize, _minCellSize, _screenPadding);
        }

        public void ExitGame()
        {
            _isGameActive = false;
            _boardView.DestroyBoard(boardOutDuration, boardOutCurve); //not performing any checks in here, assuming tweens and other code would do OnDestroy() validations at a final product scenario
            ShowMainMenu();
            HideGameMenu();
        }

        private Tween HideMainMenu()
        {
            mainMenuPanel.DOMoveX(mainMenuAnchorRef.transform.position.x + moveMenuX, hideMenuDuration).SetEase(hideMenuCurve);

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
            mainMenuPanel.DOMoveX(mainMenuAnchorRef.transform.position.x, showMenuDuration).SetEase(showMenuCurve);

            DOVirtual.Float(0.0f, 1.0f, showMenuDuration*1.5f, (value) =>
            {
                mainMenu.alpha = value;
            });

            return DOVirtual.DelayedCall(showMenuDuration, () => { });
        }

        private void HideGameMenu()
        {
            gameMenu.interactable = false;
            DOVirtual.Float(1.0f, 0.0f, hideGameMenuDuration, (value) =>
            {
                gameMenu.alpha = value;
            });
        }

        private void ShowGameMenu()
        {
            gameMenu.interactable = true;
            DOVirtual.Float(0.0f, 1.0f, showGameMenuDuration, (value) =>
            {
                gameMenu.alpha = value;
            });
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
