using System;
using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    public class UIManager : MonoSingleton<UIManager>
    {
        [SerializeField] private TMP_Text _starCountText;
        [SerializeField] private RectTransform _starIcon;
        [SerializeField] private float _starBounceScale = 1.3f;
        [SerializeField] private float _starBounceDuration = 0.3f;
        [SerializeField] private TMP_Text _gameOverText;

        private Vector3 _starIconOriginalScale;

        [Header("Hearts UI")]
        [SerializeField] private RectTransform[] _heartContainers;
        [SerializeField] private RectTransform[] _heartFills;
        [SerializeField] private float _heartBounceScale = 1.3f;
        [SerializeField] private float _heartBounceDuration = 0.3f;

        private int _previousHealth;
        private Vector3[] _containerOriginalScales;
        private Vector3[] _fillOriginalScales;

        [Header("Lives UI")]
        [SerializeField] private RectTransform _livesContainer;
        [SerializeField] private TMP_Text _livesText;
        [SerializeField] private float _slideDistance = 200f;
        [SerializeField] private float _slideDuration = 0.5f;
        [SerializeField] private float _displayDuration = 2f;

        [Header("Game Over")]
        [SerializeField] private CircleFade _circleFade;
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private float _gameOverTextDuration = 2f;

        private Vector2 _livesOnScreenPos;
        private Vector2 _livesOffScreenPos;
        private Tween _livesTween;

        private void Start()
        {
            if (_livesContainer != null)
            {
                _livesOnScreenPos = _livesContainer.anchoredPosition;
                _livesOffScreenPos = _livesOnScreenPos + new Vector2(-_slideDistance, 0);
                _livesContainer.anchoredPosition = _livesOffScreenPos;
            }

            _previousHealth = _heartFills.Length;

            if (_starIcon != null)
            {
                _starIconOriginalScale = _starIcon.localScale;
            }

            _containerOriginalScales = new Vector3[_heartContainers.Length];
            _fillOriginalScales = new Vector3[_heartFills.Length];

            for (int i = 0; i < _heartContainers.Length; i++)
            {
                _containerOriginalScales[i] = _heartContainers[i].localScale;
            }

            for (int i = 0; i < _heartFills.Length; i++)
            {
                _fillOriginalScales[i] = _heartFills[i].localScale;
            }
        }

        public void ManageHearts(int currentHealth)
        {
            for (int i = 0; i < _heartFills.Length; i++)
            {
                bool shouldBeActive = i < currentHealth;
                bool wasActive = i < _previousHealth;

                _heartFills[i].gameObject.SetActive(shouldBeActive);

                if (shouldBeActive && !wasActive)
                {
                    BounceHeart(i);
                }
            }

            _previousHealth = currentHealth;
        }

        private void BounceHeart(int index)
        {
            RectTransform container = _heartContainers[index];
            RectTransform fill = _heartFills[index];

            Vector3 containerOriginal = _containerOriginalScales[index];
            Vector3 fillOriginal = _fillOriginalScales[index];

            container.localScale = containerOriginal;
            fill.localScale = fillOriginal;

            Vector3 containerPunch = containerOriginal * (_heartBounceScale - 1f);
            Vector3 fillPunch = fillOriginal * (_heartBounceScale - 1f);

            container.DOPunchScale(containerPunch, _heartBounceDuration, 1, 0.5f);
            fill.DOPunchScale(fillPunch, _heartBounceDuration, 1, 0.5f);
        }

        public void IncreaseStarCount(int amount)
        {
            _starCountText.text = amount.ToString();

            if (_starIcon != null)
            {
                _starIcon.localScale = _starIconOriginalScale;
                Vector3 punch = _starIconOriginalScale * (_starBounceScale - 1f);
                _starIcon.DOPunchScale(punch, _starBounceDuration, 1, 0.5f);
            }
        }

        public void DisplayGameOverText()
        {
            _gameOverText.enabled = true;

            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(_gameOverTextDuration);
            sequence.AppendCallback(() =>
            {
                _gameOverText.enabled = false;
                _circleFade.PlayGameOverSequence(_playerTransform);
            });
        }

        public void UpdateLives(int lives)
        {
            if (_livesContainer == null || _livesText == null) return;

            _livesText.text = lives.ToString();

            _livesTween?.Kill();

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_livesContainer.DOAnchorPos(_livesOnScreenPos, _slideDuration).SetEase(Ease.OutBack));
            sequence.AppendInterval(_displayDuration);
            sequence.Append(_livesContainer.DOAnchorPos(_livesOffScreenPos, _slideDuration).SetEase(Ease.InBack));
            _livesTween = sequence;
        }
    }
}
