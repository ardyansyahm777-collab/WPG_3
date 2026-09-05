using UnityEngine;

namespace WpgGame.Progression
{
    /// <summary>
    /// Satu pilihan upgrade yang ditampilkan di popup level-up.
    /// Kontrak API (Bagian 8). Semua field publik (additive).
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradeOption", menuName = "WPG_3/Upgrade Option")]
    public class UpgradeOption : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Id unik, mis. \"damage_plus\"")]
        public string Id;

        [Tooltip("Nama yang tampil di UI, mis. \"Damage +1\"")]
        public string DisplayName;

        [Tooltip("Deskripsi singkat untuk popup")]
        public string Description;

        [Tooltip("Ikon opsional (boleh kosong)")]
        public Sprite Icon;

        [Header("Effect")]
        public UpgradeType Type;

        [Tooltip("Nilai yang ditambahkan (additive)")]
        public float Value;
    }
}