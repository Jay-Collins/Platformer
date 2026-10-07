using System;
using Managers;
using UnityEngine;

namespace Collectables
{
    public class Star : MonoBehaviour
    {
        [SerializeField] private float _amplitude;
        [SerializeField] private float _bounceSpeed;
        [SerializeField] private float _rotationSpeed;
        private Vector3 _startPosition;
        
        private void Start()
        {
            _startPosition = transform.position;
        }
        
        private void Update()
        {
            Movement();
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                UpdatePlayerManager();
                UpdateSoundManager();
                Destroy(gameObject);
            }
        }
        
        private void Movement()
        {
            // Bounce. Mathf.Sin makes it bounce, speed is how fast, amplitude controls the distance basically.
            float newY = _startPosition.y + Mathf.Sin(Time.time * _bounceSpeed) * _amplitude;
            transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);
            
            // Rotation.
            transform.Rotate(0, _rotationSpeed * Time.deltaTime, 0);
        }
        
        private void UpdatePlayerManager()
        {
            PlayerManager.instance.IncreaseStarCount(+1);
        }

        private void UpdateSoundManager()
        {
            SoundManager.instance.PlayStarCollect();
        }
    }
}
