using Managers;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Animator))]
    [DefaultExecutionOrder(100)]
    public class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerMovement _movement;
        [SerializeField, Min(0f)] private float _speedDamping = 0.08f;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        private static readonly int MovementEnabled = Animator.StringToHash("MovementEnabled");

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_movement == null) _movement = GetComponentInParent<PlayerMovement>();

            if (_movement == null || _animator.runtimeAnimatorController == null)
            {
                Debug.LogError("PlayerAnimation needs a PlayerMovement and an Animator controller.", this);
                enabled = false;
                return;
            }

            // The rigidbody owns movement; animation only poses the visual child.
            _animator.applyRootMotion = false;
        }

        private void Update()
        {
            float speed = _movement.CanMove ? _movement.NormalizedSpeed : 0f;
            _animator.SetFloat(Speed, speed, _speedDamping, Time.deltaTime);
            _animator.SetBool(Grounded, _movement.IsGrounded);
            _animator.SetFloat(VerticalSpeed, _movement.VerticalVelocity);
            _animator.SetBool(MovementEnabled, _movement.CanMove);
        }

        public void PlayFootstep()
        {
            if (_movement != null && _movement.CanMove && _movement.IsGrounded &&
                _movement.NormalizedSpeed > 0.1f && SoundManager.instance != null)
                SoundManager.instance.PlayFootstep();
        }
    }
}
