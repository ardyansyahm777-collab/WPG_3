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
    /// Stat lengkap 20-slot hero diisi lewat <see cref="ApplyCharacter"/> (HP lewat HealthSystem.MaxHP).
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Tooltip("Kecepatan gerak player (unit/detik). = MovementSpeed CharacterData.")]
        public float MoveSpeed = 5f;

        [Tooltip("Fire rate: jumlah tembakan per detik. = AttackSpeed CharacterData.")]
        public float FireRate = 1.1f;

        [Tooltip("Damage per proyektil. = PhysicalAttack CharacterData.")]
        public float Damage = 85f;

        [Tooltip("Jumlah proyektil yang ditembakkan sekaligus per tembakan.")]
        public int MultiShot = 1;

        [Header("Stat Hero (16) — lihat CharacterData untuk 20 slot penuh")]
        [Tooltip("Reduksi damage fisik flat.")]
        public float PhysicalDefense = 40f;

        [Tooltip("Persen, mis. 10 = 10%.")]
        public float CritChance = 10f;

        [Tooltip("Mana maksimum.")]
        public float Mana = 300f;

        public float MagicPower = 0f;
        public float MagicalDefense = 35f;

        [Tooltip("Persen, mis. 0 = tanpa reduksi cooldown.")]
        public float CooldownReduction = 0f;

        public float HPRegen = 8f;
        public float PhysicalPEN = 0f;

        [Tooltip("Persen.")]
        public float Lifesteal = 0f;

        [Tooltip("Jangkauan serangan dasar.")]
        public float BasicAttackRange = 5.5f;

        [Tooltip("Persen, mis. 200 = damage kritikal 2x.")]
        public float CritDamage = 200f;

        [Tooltip("Persen efektivitas heal, 100 = normal.")]
        public float HealEffect = 100f;

        public float ManaRegen = 5f;
        public float MagicalPEN = 0f;

        [Tooltip("Persen.")]
        public float SpellVamp = 0f;

        [Tooltip("Persen heal yang diterima, 100 = normal.")]
        public float IncomingHeal = 100f;

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

        /// <summary>
        /// Menerapkan seluruh 20 stat dari <see cref="CharacterData"/> sekaligus:
        /// HP lewat HealthSystem.MaxHP (+ heal penuh), MovementSpeed/AttackSpeed/PhysicalAttack
        /// dipetakan ke MoveSpeed/FireRate/Damage, 16 sisanya ke field stat hero.
        /// MultiShot tidak ada di CharacterData sehingga dipertahankan apa adanya.
        /// Broadcast <see cref="OnStatsChanged"/> sekali di akhir.
        /// </summary>
        public void ApplyCharacter(CharacterData data)
        {
            if (data == null) return;

            MoveSpeed = Mathf.Max(0f, data.MovementSpeed);
            FireRate = Mathf.Max(0.1f, data.AttackSpeed);
            Damage = Mathf.Max(0f, data.PhysicalAttack);

            PhysicalDefense = data.PhysicalDefense;
            CritChance = data.CritChance;
            Mana = Mathf.Max(0f, data.Mana);
            MagicPower = data.MagicPower;
            MagicalDefense = data.MagicalDefense;
            CooldownReduction = Mathf.Clamp(data.CooldownReduction, 0f, 100f);
            HPRegen = data.HPRegen;
            PhysicalPEN = data.PhysicalPEN;
            Lifesteal = data.Lifesteal;
            BasicAttackRange = Mathf.Max(0f, data.BasicAttackRange);
            CritDamage = data.CritDamage;
            HealEffect = data.HealEffect;
            ManaRegen = data.ManaRegen;
            MagicalPEN = data.MagicalPEN;
            SpellVamp = data.SpellVamp;
            IncomingHeal = data.IncomingHeal;

            var health = GetComponent<HealthSystem>();
            if (health != null && data.HP > 0f)
            {
                health.MaxHP = data.HP;
                health.Heal(data.HP); // clamp otomatis ke MaxHP = heal penuh (bila masih hidup)
            }

            OnStatsChanged?.Invoke(this);
        }
    }
}
