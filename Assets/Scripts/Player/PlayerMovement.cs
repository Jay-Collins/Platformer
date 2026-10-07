using Managers;
using UnityEngine;

namespace Player
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private LayerMask _jumpable;
        [SerializeField] private BoxCollider _boxCollider;
        [SerializeField] private Rigidbody _rigidbody;
        
        [SerializeField] private float _playerSpeed = 12f;
        [SerializeField] private float _turnSmoothingTime = 0.0025f;
        [SerializeField] private float _minJumpHeight = 2f;
        [SerializeField] private float _maxJumpHeight = 4f;
        [SerializeField] private float _gravity = -80f;
        [SerializeField] private float _deceleration = 10f;
        
        private Vector3 _direction;
        private Vector3 _inputDirection;
        private float _verticalVelocity;
        private float _currentVelocity;
        private bool _isGrounded;
        private bool _canMove = true;

        private Vector3 _knockbackVelocity;
        private bool _isKnockedBack;
        private bool _jumpRequested;
        private bool _jumpReleaseRequested;

        public bool IsGrounded => _isGrounded && _verticalVelocity <= 0f;
        public bool CanMove => _canMove;
        public float VerticalVelocity => _verticalVelocity;
        public float NormalizedSpeed => _rigidbody == null ? 0f : Mathf.Clamp01(
            new Vector2(_rigidbody.linearVelocity.x, _rigidbody.linearVelocity.z).magnitude /
            Mathf.Max(_playerSpeed, 0.001f));

        private void Awake()
        {
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void OnEnable()
        {
            InputManager.movement += MovementDirection;
            InputManager.jumpStarted += OnJumpStarted;
            InputManager.jumpCanceled += OnJumpCanceled;
        }

        private void OnDisable()
        {
            InputManager.movement -= MovementDirection;
            InputManager.jumpStarted -= OnJumpStarted;
            InputManager.jumpCanceled -= OnJumpCanceled;
            _jumpRequested = _jumpReleaseRequested = false;
            _inputDirection = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (!_canMove) return;

            ConsumeJumpInput();

            if (_isKnockedBack)
            {
                ApplyGravity();
                _rigidbody.linearVelocity = new Vector3(_knockbackVelocity.x, _verticalVelocity, _knockbackVelocity.z);
                return;
            }

            ApplyMomentum();
            ApplyGravity();
            MoveCharacter();
        }

        private void ApplyMomentum()
        {
            // Gives the character momentum when coming to a stop.
            if (_inputDirection.sqrMagnitude > 0.01f)
            {
                _direction = Vector3.Lerp(_direction, _inputDirection, _deceleration * Time.fixedDeltaTime);
            }
            else
            {
                _direction = Vector3.Lerp(_direction, Vector3.zero, _deceleration * Time.fixedDeltaTime);
            }
        }
        
        private void MoveCharacter()
        {
            // Smoothly rotates player as they move around.
            if (_direction.sqrMagnitude > 0.01f)
            {
                var targetAngle = Mathf.Atan2(_direction.x, _direction.z) * Mathf.Rad2Deg;
                var angle = Mathf.SmoothDampAngle(_rigidbody.rotation.eulerAngles.y, targetAngle,
                    ref _currentVelocity, _turnSmoothingTime, Mathf.Infinity, Time.fixedDeltaTime);
                _rigidbody.MoveRotation(Quaternion.Euler(0f, angle, 0f));
            }
            
            Vector3 horizontalVelocity = _direction * _playerSpeed;

            _rigidbody.linearVelocity = new Vector3(horizontalVelocity.x, _verticalVelocity, horizontalVelocity.z);

        }
        
        private void MovementDirection(Vector2 move)
        {
            if (!_canMove) return;
            // Receives the input from the input manager
            var cameraForward = new Vector3(_playerCamera.transform.forward.x, 0, _playerCamera.transform.forward.z).normalized;
            var cameraRight =  new Vector3(_playerCamera.transform.right.x, 0, _playerCamera.transform.right.z).normalized;
            
            _inputDirection = cameraForward * move.y + cameraRight * move.x;
        }

        private void OnJumpStarted()
        {
            if (_canMove) _jumpRequested = true;
        }

        private void OnJumpCanceled()
        {
            if (_canMove) _jumpReleaseRequested = true;
        }

        private void ConsumeJumpInput()
        {
            // Input is sampled every rendered frame; physics consumes each edge once.
            // A press and release between ticks still produces a short jump.
            if (_jumpRequested && !_isKnockedBack && IsGrounded)
            {
                _isGrounded = false;
                _verticalVelocity = Mathf.Sqrt(_maxJumpHeight * -2f * _gravity);
                SoundManager.instance.PlayJump();
            }
            if (_jumpReleaseRequested && !_isKnockedBack && _verticalVelocity > 0)
            {
                var minVelocity = Mathf.Sqrt(_minJumpHeight * -2f * _gravity);
                var releaseVelocity = Mathf.Sqrt(_minJumpHeight * 0.5f * minVelocity);
                _verticalVelocity = Mathf.Min(_verticalVelocity, releaseVelocity);
            }
            _jumpRequested = _jumpReleaseRequested = false;
        }
        
        private void ApplyGravity()
        {
            if (_isGrounded && _verticalVelocity < 0)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += _gravity * Time.fixedDeltaTime;
            }
        }
        

        private void OnCollisionEnter(Collision collision)
        {
            if (_verticalVelocity > 0f) return;

            if (((1 << collision.gameObject.layer) & _jumpable) != 0)
            {
                foreach (var contact in collision.contacts)
                {
                    if (contact.normal.y > 0.7f)
                    {
                        if (_isKnockedBack)
                        {
                            // Landing from knockback - end it
                            _isKnockedBack = false;
                            _knockbackVelocity = Vector3.zero;
                        }
                        _isGrounded = true;
                        return;
                    }
                }
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            // Don't update grounded while knocked back
            if (_isKnockedBack || _verticalVelocity > 0f) return;

            if (((1 << collision.gameObject.layer) & _jumpable) != 0)
            {
                foreach (var contact in collision.contacts)
                {
                    if (contact.normal.y > 0.7f)
                    {
                        _isGrounded = true;
                        return;
                    }
                }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & _jumpable) != 0)
            {
                _isGrounded = false;
            }
        }

        public void SetMovementEnabled(bool canMove)
        {
            _canMove = canMove;

            if (!canMove)
            {
                _jumpRequested = _jumpReleaseRequested = false;
                _direction = Vector3.zero;
                _inputDirection = Vector3.zero;
                _verticalVelocity = 0f;
                _rigidbody.linearVelocity = Vector3.zero;
                _isKnockedBack = false;
                _knockbackVelocity = Vector3.zero;
            }
        }

        public void ClearKnockback()
        {
            _isKnockedBack = false;
            _knockbackVelocity = Vector3.zero;
        }

        public void ResetMotion()
        {
            _jumpRequested = _jumpReleaseRequested = false;
            _currentVelocity = 0f;
            ClearKnockback();
            _direction = Vector3.zero;
            _inputDirection = Vector3.zero;
            _verticalVelocity = 0f;
            _isGrounded = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        public void Teleport(Vector3 position)
        {
            ResetMotion();
            // Teleports bypass interpolation instead of dragging through the level.
            _rigidbody.position = position;
            transform.position = position;
        }

        public void ApplyKnockback(Vector3 knockbackForce, bool dealsDamage = false)
        {
            if (!_canMove) return;
            _jumpRequested = _jumpReleaseRequested = false;
            _knockbackVelocity = new Vector3(knockbackForce.x, 0f, knockbackForce.z);
            _verticalVelocity = knockbackForce.y;
            _isKnockedBack = true;
            _isGrounded = false;

            _direction = Vector3.zero;
            _inputDirection = Vector3.zero;

            if (dealsDamage)
            {
                PlayerManager.instance.TakeDamage();
            }
        }
    }
}
