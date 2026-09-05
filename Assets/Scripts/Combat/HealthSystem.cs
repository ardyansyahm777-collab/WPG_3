using System;
using UnityEngine;

namespace WpgGame.Combat
{
    /// <summary>
    /// Komponen HP reusable untuk Player dan Enemy.
    /// Broadcast OnHealthChanged(current, max) setiap HP berubah, dan OnDeath sekali saat HP habis.
    /// Dipanggil lewat TakeDamage (serangan/enemy contact) dan Heal (upgrade/regen).
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        [Tooltip("HP maksimum saat mulai; juga cap atas untuk Heal.")]
        [SerializeField] public float MaxHP = 3f;

        public float CurrentHP { get; private set; }

        public bool IsDead { get; private set; }

        /// <summary>Dipanggil saat (current, max) berubah.</summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>Dipanggil tepat sekali ketika HP mencapai 0.</summary>
        public event Action OnDeath;

        private void Awake()
        {
            if (MaxHP <= 0f) MaxHP = 1f;
            CurrentHP = MaxHP;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHP = Mathf.Max(0f, CurrentHP - amount);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
            if (CurrentHP <= 0f)
            {
                IsDead = true;
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }
    }
}
