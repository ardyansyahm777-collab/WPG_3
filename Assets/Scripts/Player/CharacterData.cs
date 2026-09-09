using System;
using UnityEngine;

namespace WpgGame.Player
{
    /// <summary>
    /// Data satu hero playable (dipilih di menu / dibeli di shop oleh Prog 2/3).
    /// Berisi identitas, 20 stat float, 3 skill aktif, dan 1 pasif.
    /// Diterapkan ke runtime lewat <see cref="PlayerStats.ApplyCharacter"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "Character", menuName = "WPG_3/Character")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identitas")]
        [Tooltip("Id unik, mis. 'aelindra'.")]
        public string Id = "aelindra";

        [Tooltip("Nama tampilan, mis. 'Aelindra'.")]
        public string DisplayName = "Aelindra";

        [Tooltip("Role hero, mis. 'Archer'.")]
        public string Role = "Archer";

        [Tooltip("Inisial portrait, mis. 'AE'.")]
        public string PortraitText = "AE";

        [Tooltip("True = langsung bisa dipakai; false = harus dibuka dulu.")]
        public bool IsUnlocked = true;

        [Tooltip("Harga buka hero (gold).")]
        public int Price = 0;

        [Header("Stat (20)")]
        [Tooltip("HP maksimum.")]
        public float HP = 1200f;
        public float PhysicalAttack = 85f;
        public float PhysicalDefense = 40f;
        [Tooltip("Serangan per detik (dipetakan ke PlayerStats.FireRate).")]
        public float AttackSpeed = 1.1f;
        [Tooltip("Persen, mis. 10 = 10%.")]
        public float CritChance = 10f;
        public float Mana = 300f;
        public float MagicPower = 0f;
        public float MagicalDefense = 35f;
        [Tooltip("Persen, mis. 0 = tanpa reduksi cooldown.")]
        public float CooldownReduction = 0f;
        [Tooltip("Unit/detik (dipetakan ke PlayerStats.MoveSpeed).")]
        public float MovementSpeed = 5f;
        public float HPRegen = 8f;
        public float PhysicalPEN = 0f;
        [Tooltip("Persen.")]
        public float Lifesteal = 0f;
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

        [Header("Skill")]
        public HeroSkillData Skill1;
        public HeroSkillData Skill2;
        public HeroSkillData Skill3;
        public HeroPassiveData Passive;
    }

    /// <summary>Data satu skill aktif hero.</summary>
    [Serializable]
    public struct HeroSkillData
    {
        [Tooltip("Nama skill.")]
        public string Name;

        [Tooltip("Deskripsi efek skill.")]
        [TextArea] public string Desc;

        [Tooltip("Cooldown antar cast (detik).")]
        public float Cooldown;

        [Tooltip("Nilai efek (makna per skill, mis. multiplier damage atau jumlah heal).")]
        public float Value;
    }

    /// <summary>Data pasif hero.</summary>
    [Serializable]
    public struct HeroPassiveData
    {
        [Tooltip("Nama pasif.")]
        public string Name;

        [Tooltip("Deskripsi efek pasif.")]
        [TextArea] public string Desc;

        [Tooltip("Nilai efek pasif.")]
        public float Value;
    }
}
