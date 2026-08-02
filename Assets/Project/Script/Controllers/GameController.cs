using DG.Tweening;
using DG.Tweening.Core.Easing;
using Gazeus.DesafioMatch3.Core;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.Controllers
{
    public class GameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardView _boardView;
        [SerializeField] private CanvasGroup mainMenu;
        [SerializeField] private RectTransform mainMenuPanel;
        [SerializeField] private RectTransform mainMenuAnchorRef;
        [SerializeField] private RectTransform mainMenuButtonsHolder;
        [SerializeField] private CanvasGroup gameMenu;
        [SerializeField] private Transform BoardTitle;
        [SerializeField] private Slider themeSlider;

        [Header("Menu Animation Settings")]
        [SerializeField] private float moveMenuX = 300f;
        [SerializeField] private float showMenuDuration = 1.2f;
        [SerializeField] private Ease showMenuCurve;
        [SerializeField] private float hideMenuDuration = 1.2f;
        [SerializeField] private Ease hideMenuCurve;
        [SerializeField] private float shrinkButtonPerc = 0.5f;
        [SerializeField] private float shrinkButtonDuration = 0.5f;
        [SerializeField] private float perButtonDelay = 0.2f;
        [SerializeField] private Ease shrinkButtonCurve;
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
        [SerializeField] private float dissolveDurationCombo = 0.2f;
        [SerializeField] private Ease dissolveCurve;
        [SerializeField] private Ease dissolveShaderCurve;
        [SerializeField] private Vector3 comboRotation;
        [SerializeField] private Ease comboRotateCurve;
        [SerializeField] private float bounceBGDuration = 0.3f;
        [SerializeField] private Ease bounceBGCurve;
        [SerializeField] private float shineBGDuration = 0.3f;
        [SerializeField] private Ease shineBGCurve;
        [SerializeField] private ParticleSystem destroyParticle;
        [SerializeField] private ParticleSystem ParticleCombo_Left;
        [SerializeField] private ParticleSystem ParticleCombo_Right;
        [SerializeField] private float particleDelayPerc = 0.8f;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private Ease moveCurve;
        [SerializeField] private float wrongMovePunchScale = 1.1f;
        [SerializeField] private float wrongMovePunchDuration = 0.1f;

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
            Shader.SetGlobalFloat("_BounceBG", 0.0f);
            Shader.SetGlobalFloat("_Theme", 0.0f);
            themeSlider.onValueChanged.RemoveListener(OnThemeSliderChanged);
        }

        private void Start()
        {
            _isGameActive = false;

            themeSlider.onValueChanged.AddListener(OnThemeSliderChanged);
            Shader.SetGlobalFloat("_BounceBG", 0.0f);

            gameMenu.alpha = 0.0f;
            gameMenu.interactable = false;

            mainMenuPanel.transform.position = new Vector3(mainMenuAnchorRef.transform.position.x + moveMenuX, 
                mainMenuPanel.transform.position.y, mainMenuPanel.transform.position.z);
            ShowMainMenu();
        }

        void OnThemeSliderChanged(float value)
        {
            Shader.SetGlobalFloat("_Theme", value);
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
            _boardView.DestroyBoard(boardOutDuration, boardOutCurve); //not performing any checks in here, assuming tweens and other code would do OnDestroy() validations at a final product
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

            int i = 0;
            foreach (Transform child in mainMenuButtonsHolder)
            {
                float shrinkSize = child.GetComponent<RectTransform>().sizeDelta.x * shrinkButtonPerc;
                DOVirtual.Float(-shrinkSize, 0.0f, shrinkButtonDuration + (i * perButtonDelay), (value) =>
                {
                    var img = child.GetChild(0).GetComponent<RectTransform>();
                    img.sizeDelta = new Vector2(value ,img.sizeDelta.y);
                });
                i++;
            }

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

            int combinations = boardSequence.MatchedPosition.Count;
            float duration = combinations > 3 ? dissolveDurationCombo : dissolveDuration;

            _boardView.ComboEffects(boardSequence.MatchedPosition, dissolveScale, duration, dissolveCurve, dissolveShaderCurve, comboRotation, comboRotateCurve, destroyParticle, particleDelayPerc,
                bounceBGDuration, bounceBGCurve, shineBGDuration, shineBGCurve)
                .onComplete += () =>
            {
                if (combinations > 6)
                {
                    ParticleCombo_Left.Play();
                    ParticleCombo_Right.Play();
                }
                Sequence sequence = DOTween.Sequence();
                sequence.Append(_boardView.DestroyTiles(boardSequence.MatchedPosition));
                sequence.Append(_boardView.MoveTiles(boardSequence.MovedTiles, moveDuration, moveCurve, wrongMovePunchScale, wrongMovePunchDuration));
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
                    _boardView.SwapTiles(_selectedX, _selectedY, x, y, true, moveDuration, moveCurve, wrongMovePunchScale, wrongMovePunchDuration).onComplete += () =>
                    {
                        bool isValid = _gameService.IsValidMovement(_selectedX, _selectedY, x, y);
                        if (isValid)
                        {
                            List<BoardSequence> swapResult = _gameService.SwapTile(_selectedX, _selectedY, x, y);
                            AnimateBoard(swapResult, 0, () => _isAnimating = false);
                        }
                        else
                        {
                            _boardView.SwapTiles(x, y, _selectedX, _selectedY, false, moveDuration, moveCurve, wrongMovePunchScale, wrongMovePunchDuration)
                            .onComplete += () => _isAnimating = false;
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
