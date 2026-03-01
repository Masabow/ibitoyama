using System;
using UnityEngine;

namespace Ibitoyama.BodyShooter
{
  public class PlayerHealth : MonoBehaviour
  {
    [SerializeField] private int maxHp = 3;
    [SerializeField] private float invincibleSec = 0.8f;

    public event Action<int> OnHpChanged;
    public event Action OnDead;

    private float _invincibleUntil;

    public int CurrentHp { get; private set; }
    public bool IsDead => CurrentHp <= 0;

    private void Awake()
    {
      ResetHealth();
    }

    public void ResetHealth()
    {
      CurrentHp = Mathf.Max(1, maxHp);
      _invincibleUntil = 0f;
      OnHpChanged?.Invoke(CurrentHp);
    }

    public bool TakeDamage(int damage)
    {
      if (IsDead || Time.time < _invincibleUntil || damage <= 0)
      {
        return false;
      }

      CurrentHp = Mathf.Max(0, CurrentHp - damage);
      _invincibleUntil = Time.time + invincibleSec;
      OnHpChanged?.Invoke(CurrentHp);

      if (CurrentHp == 0)
      {
        OnDead?.Invoke();
      }

      return true;
    }
  }
}
