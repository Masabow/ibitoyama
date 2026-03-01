using UnityEngine;

namespace Ibitoyama.BodyShooter
{
  public class DamageOnContact : MonoBehaviour
  {
    [SerializeField] private int damage = 1;
    [SerializeField] private bool destroyOnHit = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
      TryApplyDamage(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
      TryApplyDamage(other.gameObject);
    }

    private void TryApplyDamage(GameObject other)
    {
      if (!other.TryGetComponent<PlayerHealth>(out var playerHealth))
      {
        return;
      }

      if (playerHealth.TakeDamage(damage) && destroyOnHit)
      {
        Destroy(gameObject);
      }
    }
  }
}
