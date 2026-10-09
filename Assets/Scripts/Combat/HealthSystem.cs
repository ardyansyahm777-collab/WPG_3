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

        /// <summary>Shield aktif Aelindra: menyerap damage sebelum HP. Refresh (bukan stack).</summary>
        public float ShieldPoints { get; private set; }

        /// <summary>Shield maksimum dari grant terakhir (0 = tidak ada shield).</summary>
        public float ShieldMax { get; private set; }

        /// <summary>Dipanggil saat (shieldSekarang, shieldMax) berubah.</summary>
        public event Action<float, float> OnShieldChanged;

        private void Awake()
        {
            if (MaxHP <= 0f) MaxHP = 1f;
            CurrentHP = MaxHP;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            // Shield terkuras dulu; overflow diteruskan ke HP.
            float remaining = amount;
            if (ShieldPoints > 0f)
            {
                float absorbed = Mathf.Min(ShieldPoints, remaining);
                ShieldPoints -= absorbed;
                remaining -= absorbed;
                OnShieldChanged?.Invoke(ShieldPoints, ShieldMax);
            }

            if (remaining <= 0f) return;

            CurrentHP = Mathf.Max(0f, CurrentHP - remaining);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
            if (CurrentHP <= 0f)
            {
                IsDead = true;
                OnDeath?.Invoke();
            }
        }

        /// <summary>
        /// Beri shield (ADITIF Aelindra): me-refresh nilai lama, bukan menumpuk.
        /// GrantShield(0) = kosongkan sisa shield (dipakai saat durasi shield habis).
        /// Diabaikan bila sudah mati. Shield TIDAK regen dan tidak tersentuh Heal.
        /// </summary>
        public void GrantShield(float amount)
        {
            if (IsDead) return;
            ShieldMax = Mathf.Max(0f, amount);
            ShieldPoints = ShieldMax;
            OnShieldChanged?.Invoke(ShieldPoints, ShieldMax);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        /// <summary>
        /// Kembalikan HP penuh dan hapus status mati. Dipakai object pool saat instance dipakai ulang.
        /// (Additive terhadap kontrak API: tidak mengubah signature yang ada.)
        /// </summary>
        public void ResetHealth()
        {
            IsDead = false;
            CurrentHP = MaxHP;
            // Shield tidak diwariskan antar pemakaian pool: selalu mulai dari nol.
            ShieldPoints = 0f;
            ShieldMax = 0f;
            OnShieldChanged?.Invoke(ShieldPoints, ShieldMax);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }
    }
}
