using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Managers
{
    public class InputManager : MonoSingleton<InputManager>
    {
        public static Action<Vector2> movement;
        public static Action jumpStarted;
        public static Action jumpCanceled;
        
        private PlayerInputActions _playerInput;

        private void OnEnable()
        {
            if (_playerInput == null) InitializeActionMap();
            InitializeGeneralInput();
        }

        private void OnDisable()
        {
            if (_playerInput == null) return;
            _playerInput.General.Jump.started -= OnJumpStarted;
            _playerInput.General.Jump.canceled -= OnJumpCanceled;
            _playerInput.Disable();
            movement?.Invoke(Vector2.zero);
        }

        private void OnDestroy()
        {
            _playerInput?.Dispose();
        }

        private void Update()
        {
            Movement();
        }

        private void InitializeActionMap()
        {
            _playerInput = new PlayerInputActions();
        }

        public void InitializeGeneralInput()
        {
            _playerInput.General.Jump.started -= OnJumpStarted;
            _playerInput.General.Jump.canceled -= OnJumpCanceled;
            _playerInput.General.Jump.started += OnJumpStarted;
            _playerInput.General.Jump.canceled += OnJumpCanceled;
            _playerInput.General.Enable();
        }

        private void OnJumpStarted(InputAction.CallbackContext context) => jumpStarted?.Invoke();
        private void OnJumpCanceled(InputAction.CallbackContext context) => jumpCanceled?.Invoke();

        private void Movement()
        {
            var move = _playerInput.General.Movement.ReadValue<Vector2>();
            movement?.Invoke(move);
        }
    }
}
