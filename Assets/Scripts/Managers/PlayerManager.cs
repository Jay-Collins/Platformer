using System;
using Player;
using CameraSystem;
using UnityEngine;

namespace Managers
{
    public class PlayerManager : MonoSingleton<PlayerManager>
    {
        [SerializeField] private GameObject _respawnPointContainer;
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private int _playerHealth = 3;
        [SerializeField] private int _playerLives = 3;
        [SerializeField] private int _playerMaxHealth = 3;
        private Vector3 _currentCheckPoint;
        private int _lastCheckPointOrder = -1;
        private int _starCount;

        private void Start()
        {
            _currentCheckPoint = transform.position;
            UIManager.instance.UpdateLives(_playerLives);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("KillBox"))
            {
                _playerHealth--;
                UIManager.instance.ManageHearts(_playerHealth);

                if (_playerHealth <= 0)
                {
                    OnDeath();
                }
                else
                {
                    SoundManager.instance.PlayPlayerHurt();
                    Respawn();
                    SoundManager.instance.PlayRespawn();
                }
            }
        }

        private void SubtractPlayerHealth()
        {
            _playerHealth--;
            UIManager.instance.ManageHearts(_playerHealth);

            if (_playerHealth <= 0)
            {
                OnDeath();
            }
        }

        public void TakeDamage()
        {
            SoundManager.instance.PlayPlayerHurt();
            SubtractPlayerHealth();
        }
        
        private void SubtractPlayerLives()
        {
            _playerLives--;
            UIManager.instance.UpdateLives(Mathf.Max(0, _playerLives));
            if (_playerLives < 0)
            {
                OnGameOver();
            }
        }

        public void IncreasePlayerHealth(int amount)
        {
            _playerHealth = Mathf.Min(_playerHealth + amount, _playerMaxHealth);
            UIManager.instance.ManageHearts(_playerHealth);
        }

        public void IncreasePlayerLives(int amount)
        {
            _playerLives += amount;
            UIManager.instance.UpdateLives(Mathf.Max(0, _playerLives));
        }

        private void Respawn()
        {
            if (_respawnPointContainer.transform.childCount == 0)
            {
                Debug.LogWarning("No respawn points found!");
                return;
            }

            Vector3 closestPoint = default;
            var closestDistance = float.MaxValue;
            foreach (Transform child in _respawnPointContainer.transform)
            {
                // Compare only horizontal distance from player to respawn point.
                Vector3 playerPos = transform.position;
                Vector3 spawnPos = child.position;
                var distance = Vector2.Distance(new Vector2(playerPos.x, playerPos.z), new Vector2(spawnPos.x, spawnPos.z));

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = child.position;
                }
            }

            TeleportTo(closestPoint);
        }

        private void TeleportTo(Vector3 position)
        {
            _playerMovement.Teleport(position);
            FindFirstObjectByType<CameraFollow>()?.SnapToTarget();
        }

        private void CheckPointRespawn()
        {
            TeleportTo(_currentCheckPoint);
            _playerHealth = _playerMaxHealth;
        }

        private void OnDeath()
        {
            SoundManager.instance.PlayPlayerDeath();
            SubtractPlayerLives();
            TeleportTo(_currentCheckPoint);

            if (_playerLives >= 0)
            {
                SoundManager.instance.PlayCheckpointRespawn();
                _playerHealth = _playerMaxHealth;
                UIManager.instance.ManageHearts(_playerHealth);
            }
        }

        private void OnGameOver()
        {
            SoundManager.instance.PlayGameOver();
            _playerMovement.SetMovementEnabled(false);
            UIManager.instance.DisplayGameOverText();
        }

        public void SetCheckpoint(Vector3 checkPointPosition, int checkPointOrder)
        {
            if (checkPointOrder > _lastCheckPointOrder)
            {
                _currentCheckPoint = checkPointPosition;
                _lastCheckPointOrder = checkPointOrder;
            }
        }

        public void IncreaseStarCount(int amount)
        {
            _starCount += amount;
            UIManager.instance.IncreaseStarCount(_starCount);
        }
    }
}
