using UnityEngine;

namespace Hazards
{
    public class Hammer : MonoBehaviour
    {
        [Header("Rotation")]
        [SerializeField] private float _spinSpeed = 90f;
        [SerializeField] private Vector3 _rotationAxis = Vector3.up;

        [Header("Knockback")]
        [SerializeField] private float _knockbackForce = 20f;
        [SerializeField] private float _verticalKnockbackMultiplier = 0.5f;
        [SerializeField] private bool _dealsDamage = true;

        [Header("References")]
        [SerializeField] private Transform _pivot;

        private void Update()
        {
            Vector3 pivotPoint = _pivot != null ? _pivot.position : transform.position;
            transform.RotateAround(pivotPoint, _rotationAxis, _spinSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                ApplyKnockback(other);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                ApplyKnockback(collision);
            }
        }

        private void ApplyKnockback(Collider playerCollider)
        {
            var playerMovement = playerCollider.GetComponent<Player.PlayerMovement>();
            if (playerMovement == null)
            {
                playerMovement = playerCollider.GetComponentInParent<Player.PlayerMovement>();
            }

            if (playerMovement != null)
            {
                Vector3 horizontalDir = playerCollider.transform.position - transform.position;
                horizontalDir.y = 0;
                horizontalDir.Normalize();

                Vector3 knockback = horizontalDir * _knockbackForce;
                knockback.y = _knockbackForce * _verticalKnockbackMultiplier;

                playerMovement.ApplyKnockback(knockback, _dealsDamage);
            }
        }

        private void ApplyKnockback(Collision collision)
        {
            var playerMovement = collision.gameObject.GetComponent<Player.PlayerMovement>();
            if (playerMovement == null)
            {
                playerMovement = collision.gameObject.GetComponentInParent<Player.PlayerMovement>();
            }

            if (playerMovement != null)
            {
                // Use collision normal - the direction the player is being pushed
                Vector3 normal = collision.contacts[0].normal;
                Vector3 horizontalDir = new Vector3(normal.x, 0f, normal.z);

                // If normal is mostly vertical, use direction from hammer to player instead
                if (horizontalDir.sqrMagnitude < 0.1f)
                {
                    horizontalDir = collision.transform.position - transform.position;
                    horizontalDir.y = 0f;
                }

                horizontalDir.Normalize();

                Vector3 knockback = horizontalDir * _knockbackForce;
                knockback.y = _knockbackForce * _verticalKnockbackMultiplier;

                playerMovement.ApplyKnockback(knockback, _dealsDamage);
            }
        }
    }
}
