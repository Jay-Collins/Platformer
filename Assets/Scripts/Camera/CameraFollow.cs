using UnityEngine;

namespace CameraSystem
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothSpeed = 5f;

        private Vector3 _offset;
        private Vector3 _velocity;

        private void Start()
        {
            if (_target != null)
            {
                _offset = transform.position - _target.position;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 targetPosition = _target.position + _offset;
            // Read the rendered, interpolated transform after physics and animation.
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition,
                ref _velocity, 1f / Mathf.Max(_smoothSpeed, 0.01f), Mathf.Infinity, Time.deltaTime);
        }

        public void SnapToTarget()
        {
            if (_target == null) return;
            _velocity = Vector3.zero;
            transform.position = _target.position + _offset;
        }
    }
}
