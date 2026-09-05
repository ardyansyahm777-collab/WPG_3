using System;
using UnityEngine;
using WpgGame.Combat;
using WpgGame.Progression;

namespace WpgGame.Player
{
    /// <summary>
    /// Statistik inti player: kecepatan, fire rate, damage, dan multishot.
    /// Disunting lewat inspector dan di-upgrade lewat <see cref="ApplyUpgrade"/> (dipanggil UpgradeSystem).
    /// Setiap perubahan membroadcast <see cref="OnStatsChanged"/> agar sistem lain (shooter, HUD) ikut re-apply.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Tooltip("Kecepatan gerak player (unit/detik).")]
        public float MoveSpeed = 5f;

        [Tooltip("Fire rate: jumlah tembakan per detik.")]
        public float FireRate = 3f;

        [Tooltip("Damage per proyektil.")]
        public float Damage = 1f;

        [Tooltip("Jumlah proyektil yang ditembakkan sekaligus per tembakan.")]
        public int MultiShot = 1;

        /// <summary>Broadcast setiap ada stat yang berubah (arg: komponen ini).</summary>
        public event Action<PlayerStats> OnStatsChanged;

        /// <summary>
        /// Menerapkan upgrade (additive) sesuai tipe. Nilai resisten: MoveSpeed/FireRate/Damage tidak pernah negatif
        /// (FireRate di-clamp minimal 0.1 supaya &lt;0 tidak membalik arah cooldown), MultiShot minimal 1.
        /// Untuk MaxHealth: tambah MaxHP dan Heal senilai kenaikannya supaya player merasa efeknya langsung.
        /// </summary>
        public void ApplyUpgrade(UpgradeType type, float value)
        {
            switch (type)
            {
                case UpgradeType.MaxHealth:
                {
                    var health = GetComponent<HealthSystem>();
                    if (health != null && value > 0f)
                    {
                        health.MaxHP += value;
                        health.Heal(value);
                    }
                    break;
                }
                case UpgradeType.MoveSpeed:
                    MoveSpeed = Mathf.Max(0f, MoveSpeed + value);
                    break;
                case UpgradeType.FireRate:
                    FireRate = Mathf.Max(0.1f, FireRate + value);
                    break;
                case UpgradeType.Damage:
                    Damage = Mathf.Max(0f, Damage + value);
                    break;
                case UpgradeType.MultiShot:
                    MultiShot = Mathf.Max(1, MultiShot + Mathf.RoundToInt(value));
                    break;
            }

            OnStatsChanged?.Invoke(this);
        }
    }
}
