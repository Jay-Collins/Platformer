using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class CircleFade : MonoBehaviour
    {
        [Tooltip("RawImage with a texture that has a transparent circular hole in the center")]
        [SerializeField] private RectTransform _circleImage;
        [Tooltip("Solid black RawImage that fades in for the final blackout")]
        [SerializeField] private RawImage _blackOverlay;
        [SerializeField] private Camera _camera;
        [SerializeField] private float _startScale = 10f;
        [SerializeField] private float _pauseScale = 1f;
        [SerializeField] private float _fadeToPlayerDuration = 1f;
        [SerializeField] private float _pauseDuration = 2f;
        [SerializeField] private float _fadeToBlackDuration = 0.5f;

        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_camera == null) _camera = Camera.main;

            _circleImage.localScale = Vector3.one * _startScale;
            gameObject.SetActive(false);
        }

        public void PlayGameOverSequence(Transform target, Action onComplete = null)
        {
            gameObject.SetActive(true);
            _blackOverlay.color = new Color(0, 0, 0, 0);
            _circleImage.localScale = Vector3.one * _startScale;

            UpdatePosition(target);

            Sequence sequence = DOTween.Sequence();

            sequence.Append(_circleImage.DOScale(_pauseScale, _fadeToPlayerDuration).SetEase(Ease.InOutSine));
            sequence.AppendInterval(_pauseDuration);
            sequence.Append(_blackOverlay.DOFade(1f, _fadeToBlackDuration).SetEase(Ease.InSine));
            sequence.AppendCallback(() => onComplete?.Invoke());
        }

        private void UpdatePosition(Transform target)
        {
            if (target == null || _camera == null) return;

            Vector3 screenPos = _camera.WorldToScreenPoint(target.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                screenPos,
                _canvas.worldCamera,
                out Vector2 localPoint
            );

            _circleImage.localPosition = localPoint;
        }
    }
}
