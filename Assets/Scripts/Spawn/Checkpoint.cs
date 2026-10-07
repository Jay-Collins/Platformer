using Managers;
using UnityEngine;

namespace Spawn
{
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private GameObject _littleFlag;
        [SerializeField] private GameObject _bigFlag;
        [SerializeField] private int _checkpointOrder;
        [SerializeField] private Transform _spawnPoint;
        private bool _isActive = false;

        private void OnTriggerEnter(Collider other)
        {
            if (_isActive) return;

            if (other.CompareTag("Player"))
            {
                _littleFlag.SetActive(false);
                _bigFlag.SetActive(true);
                _isActive = true;

                Vector3 spawnPosition = _spawnPoint != null ? _spawnPoint.position : transform.position;
                PlayerManager.instance.SetCheckpoint(spawnPosition, _checkpointOrder);
                SoundManager.instance.PlayCheckpointActivate();
            }
        }
    }
}
