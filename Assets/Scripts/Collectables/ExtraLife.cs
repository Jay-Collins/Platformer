using Managers;
using UnityEngine;

namespace Collectables
{
    public class ExtraLife : MonoBehaviour
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
                PlayerManager.instance.IncreasePlayerLives(1);
                SoundManager.instance.PlayExtraLife();
                Destroy(gameObject);
            }
        }

        private void Movement()
        {
            float newY = _startPosition.y + Mathf.Sin(Time.time * _bounceSpeed) * _amplitude;
            transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);

            transform.Rotate(0, _rotationSpeed * Time.deltaTime, 0);
        }
    }
}
