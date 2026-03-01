using UnityEngine;

namespace Ibitoyama.BodyShooter
{
  public class EnemyMover2D : MonoBehaviour
  {
    [SerializeField] private float speed = 3.0f;
    [SerializeField] private float destroyY = -6.5f;

    private void Update()
    {
      transform.position += Vector3.down * (speed * Time.deltaTime);
      if (transform.position.y <= destroyY)
      {
        Destroy(gameObject);
      }
    }
  }
}
