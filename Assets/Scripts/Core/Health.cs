using System;
using UnityEngine;

namespace Game01
{
    /// <summary>Vida genérica para jugador y enemigos.</summary>
    public class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] int maxHealth = 100;

        public int Max => maxHealth;
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;

        /// <summary>(actual, máximo)</summary>
        public event Action<int, int> Changed;
        public event Action Died;

        void Awake() => Current = maxHealth;

        public void SetMax(int value, bool refill = true)
        {
            maxHealth = Mathf.Max(1, value);
            Current = refill ? maxHealth : Mathf.Min(Current, maxHealth);
            Changed?.Invoke(Current, maxHealth);
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0)
                return;
            Current = Mathf.Max(0, Current - amount);
            Changed?.Invoke(Current, maxHealth);
            if (Current == 0)
                Died?.Invoke();
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0)
                return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(Current, maxHealth);
        }

        public void ResetHealth()
        {
            Current = maxHealth;
            Changed?.Invoke(Current, maxHealth);
        }
    }
}
