using DG.Tweening;
using DG.Tweening.Core.Easing;
using Gazeus.DesafioMatch3.Core;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI;

namespace Gazeus.DesafioMatch3.Controllers
{
    public class GameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardView _boardView;
        [SerializeField] private CanvasGroup _mainMenu;
        [SerializeField] private RectTransform _mainMenuPanel;
        [SerializeField] private RectTransform _mainMenuAnchorRef;
        [SerializeField] private RectTransform _mainMenuButtonsHolder;
        [SerializeField] private CanvasGroup _gameMenu;
        [SerializeField] private Transform _boardTitle;
        [SerializeField] private CanvasGroup _themeMenu;
        [SerializeField] private Volume _postProcessVolume;
        [SerializeField] private ParticleSystem _destroyParticle;
        [SerializeField] private ParticleSystem _particleComboRow_Left;
        [SerializeField] private ParticleSystem _particleComboRow_Right;
        [SerializeField] private ParticleSystem _particleCombo_Left;
        [SerializeField] private ParticleSystem _particleCombo_Right;

        [Header("Board Config")]
        [SerializeField] private int _maxCellSize = 80;
        [SerializeField] private int _minCellSize = 24;
        [SerializeField] private int _screenPadding = 160;

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
        [SerializeField] private float particleDelayPerc = 0.8f;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private Ease moveCurve;
        [SerializeField] private float wrongMovePunchScale = 1.1f;
        [SerializeField] private float wrongMovePunchDuration = 0.1f;

        [Header("Visual Settings")]
        [SerializeField] private float bloom_themeA = 2f;
        [SerializeField] private float bloom_themeB = 8f;
        [SerializeField] private float chromaticDuration = 8f;
        [SerializeField] private float chromaticValue = 8f;
        [SerializeField] private Ease chromaticCurve;

        private GameService _gameService;

        private int _boardHeight = 10;
        private int _boardWidth = 10;

        private bool _isAnimating;
        private int _selectedX = -1;
        private int _selectedY = -1;
        private bool _isGameActive = false;

        private Bloom bloomEffect;
        private ChromaticAberration chromaticEffect;
        private ColorAdjustments colorEffect;

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
        }

        private void Start()
        {
            _isGameActive = false;

            Shader.SetGlobalFloat("_BounceBG", 0.0f);

            _gameMenu.alpha = 0.0f;
            _gameMenu.interactable = false;
            _themeMenu.alpha = 0.0f;
            _themeMenu.interactable = false;

            _mainMenuPanel.transform.position = new Vector3(_mainMenuAnchorRef.transform.position.x + moveMenuX, 
                _mainMenuPanel.transform.position.y, _mainMenuPanel.transform.position.z);
            StartGame();
        }

        private Tween StartGame()
        {
            _postProcessVolume.profile.TryGet<ColorAdjustments>(out colorEffect);

            return DOVirtual.Color(Color.black, Color.white, 0.5f, (value) =>
            {
                colorEffect.colorFilter.value = value;
            }).OnComplete(() => { 
                ShowMainMenu().OnComplete(() => ShowThemeMenu());
                });
        }

        private void ShowThemeMenu()
        {
            _themeMenu.interactable = true;
            DOVirtual.Float(0.0f, 1.0f, showGameMenuDuration, (value) =>
            {
                _themeMenu.alpha = value;
            });
        }

        public void ChangeTheme()
        {
            float value = 1.0f - Shader.GetGlobalFloat("_Theme");
            Shader.SetGlobalFloat("_Theme", value);

            if (_postProcessVolume.profile.TryGet<Bloom>(out bloomEffect))
            {
                bloomEffect.intensity.value = Mathf.Lerp(bloom_themeA, bloom_themeB, value);
            }

            if (_postProcessVolume.profile.TryGet<ChromaticAberration>(out chromaticEffect))
            {
                DOVirtual.Float(0.0f, chromaticValue, chromaticDuration, (value) =>
                {
                    chromaticEffect.intensity.value = value;
                }).SetEase(chromaticCurve).SetLoops(2, LoopType.Yoyo);
            }
        }

        public void UpdateBoardTitle(string name)
        {
            _boardTitle.GetComponent<TextMeshProUGUI>().text = name;
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

            int variations = 1;
            if (size <= 5) variations = 0;
            else if (size >= 8) variations = 2;

            List<List<Tile>> board = _gameService.StartGame(_boardWidth, _boardHeight, variations);
            _boardView.CreateBoard(board, boardInDuration, boardInCurve);
            _boardView.AdjustCellSize(_boardWidth, _boardHeight, _maxCellSize, _minCellSize, _screenPadding);
        }

        public void ExitGame()
        {
            _isGameActive = false;
            _boardView.DestroyBoard(boardOutDuration, boardOutCurve);
            ShowMainMenu();
            HideGameMenu();
        }

        private Tween HideMainMenu()
        {
            _mainMenuPanel.DOMoveX(_mainMenuAnchorRef.transform.position.x + moveMenuX, hideMenuDuration).SetEase(hideMenuCurve);

            DOVirtual.Float(1.0f, 0.0f, hideMenuDuration, (value) =>
            {
                _mainMenu.alpha = value;
            });

            return DOVirtual.DelayedCall(hideMenuDuration, () => {
                _mainMenu.gameObject.SetActive(false);
            });
        }

        private Tween ShowMainMenu()
        {
            _mainMenu.gameObject.SetActive(true);
            _mainMenuPanel.DOMoveX(_mainMenuAnchorRef.transform.position.x, showMenuDuration).SetEase(showMenuCurve);

            DOVirtual.Float(0.0f, 1.0f, showMenuDuration*1.5f, (value) =>
            {
                _mainMenu.alpha = value;
            });

            int i = 0;
            foreach (Transform child in _mainMenuButtonsHolder)
            {
                var img = child.GetChild(0).GetComponent<RectTransform>();
                float shrinkSize = child.GetComponent<RectTransform>().sizeDelta.x * shrinkButtonPerc;
                img.sizeDelta = new Vector2(-shrinkSize, img.sizeDelta.y);

                DOVirtual.Float(-shrinkSize, 0.0f, shrinkButtonDuration + (i * perButtonDelay), (value) =>
                {
                    img.sizeDelta = new Vector2(value ,img.sizeDelta.y);
                });
                i++;
            }

            return DOVirtual.DelayedCall(showMenuDuration, () => { });
        }

        private void HideGameMenu()
        {
            _gameMenu.interactable = false;
            DOVirtual.Float(1.0f, 0.0f, hideGameMenuDuration, (value) =>
            {
                _gameMenu.alpha = value;
            });
        }

        private void ShowGameMenu()
        {
            _gameMenu.interactable = true;
            DOVirtual.Float(0.0f, 1.0f, showGameMenuDuration, (value) =>
            {
                _gameMenu.alpha = value;
            });
        }

        private void AnimateBoard(List<BoardSequence> boardSequences, int index, Action onComplete)
        {
            BoardSequence boardSequence = boardSequences[index];

            int combinations = boardSequence.MatchedPosition.Count;

            //Not performing type (color) matching checking for combined tiles since the BoardSequence is only providing the X,Y coords of matching tiles
            //so I'm not sure if the intended design to determine combos is to consider only the total amount of tiles matched, or the total amount of same color, or even the same color that are adjanced to one another
            //for this reason, and since I don't think this is necessarily the intention of the Tech Art test, I'm moving foward with the simplest method that is checking only the total amount of tiles to check for combos
            float duration = combinations > 3 ? dissolveDurationCombo : dissolveDuration;

            if(boardSequence.rowComboID != -1)
            {
                float particleY = _boardView.transform.GetChild(_boardHeight * boardSequence.rowComboID).GetComponent<Transform>().position.y;
                _particleComboRow_Left.transform.position = new Vector3(_particleComboRow_Left.transform.position.x, particleY, 0.0f);
                _particleComboRow_Right.transform.position = new Vector3(_particleComboRow_Right.transform.position.x, particleY, 0.0f);

                _particleComboRow_Left.Play();
                _particleComboRow_Right.Play();
            }

            DOVirtual.Float(0f, 1f, bounceBGDuration, (value) => {
                Shader.SetGlobalFloat("_BounceBG", value);
            }).SetEase(bounceBGCurve).SetLoops(2, LoopType.Yoyo);

            _gameMenu.interactable = false; //avoiding tween erros due to not treating OnDestroy events atm

            _boardView.ComboEffects(boardSequence.MatchedPosition, dissolveScale, duration, dissolveCurve, dissolveShaderCurve, comboRotation, comboRotateCurve, _destroyParticle, particleDelayPerc,
                bounceBGDuration, bounceBGCurve, shineBGDuration, shineBGCurve)
                .onComplete += () =>
            {
                if (combinations > 5 && boardSequence.rowComboID == -1)
                {
                    _particleCombo_Left.Play();
                    _particleCombo_Right.Play();
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

                sequence.onComplete += () =>
                {
                    _gameMenu.interactable = true;
                };
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
