using System;
using System.Collections.Generic;
using UnityEngine;
using WpgGame.Core;
using WpgGame.Player;
using Random = UnityEngine.Random;

namespace WpgGame.Progression
{
    /// <summary>
    /// Mengelola pool upgrade, melempar N pilihan saat level up, dan menerapkan
    /// upgrade yang dipilih ke PlayerStats. Kontrak API (Bagian 9).
    /// Alur: LevelUpSystem -> GameEvents.RaiseLevelUp() -> HandleLevelUp di sini ->
    /// simpan LastRoll lalu beri tahu UI lewat event OnLevelUp.
    /// </summary>
    public class UpgradeSystem : MonoBehaviour
    {
        [Header("Pool & Roll")]
        [Tooltip("Semua UpgradeOption yang bisa dipilih pemain.")]
        public UpgradeOption[] Pool;

        [Tooltip("Jumlah pilihan yang di-roll saat level up.")]
        public int ChoicesPerLevelUp = 3;

        [Header("Player Stats")]
        [Tooltip("Opsional: isi langsung di inspector. Jika kosong, dicari otomatis lewat FindFirstObjectByType.")]
        [SerializeField] private PlayerStats playerStats;

        /// <summary>Hasil roll terakhir, dibaca oleh UI popup.</summary>
        public IReadOnlyList<UpgradeOption> LastRoll { get; private set; } = Array.Empty<UpgradeOption>();

        /// <summary>UI subscribe -> tampilkan popup pilihan upgrade.</summary>
        public event Action OnLevelUp;

        /// <summary>Dipanggil setelah upgrade berhasil diterapkan.</summary>
        public event Action<UpgradeOption> OnUpgradeApplied;

        private void OnEnable()
        {
            GameEvents.OnLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            GameEvents.OnLevelUp -= HandleLevelUp;
        }

        private void HandleLevelUp()
        {
            RollChoices();
            OnLevelUp?.Invoke();
        }

        /// <summary>
        /// Return acak <see cref="ChoicesPerLevelUp"/> pilihan dari <see cref="Pool"/>
        /// (tanpa duplikat; jika pool lebih kecil, kembalikan semua yang ada).
        /// </summary>
        public UpgradeOption[] RollChoices()
        {
            if (Pool == null || Pool.Length == 0)
            {
                LastRoll = Array.Empty<UpgradeOption>();
                return LastRoll.ToArray();
            }

            int count = Mathf.Min(ChoicesPerLevelUp, Pool.Length);
            var bucket = new List<UpgradeOption>(Pool);

            // Fisher-Yates shuffle tanpa duplikat.
            for (int i = 0; i < bucket.Count - 1; i++)
            {
                int j = Random.Range(i, bucket.Count);
                (bucket[i], bucket[j]) = (bucket[j], bucket[i]);
            }

            LastRoll = bucket.GetRange(0, count);
            return LastRoll.ToArray();
        }

        /// <summary>
        /// Menerapkan pilihan pemain: PlayerStats.ApplyUpgrade(UpgradeType, value)
        /// lalu broadcast OnUpgradeApplied.
        /// </summary>
        public void ChooseUpgrade(UpgradeOption choice)
        {
            if (choice == null)
            {
                Debug.LogWarning("[UpgradeSystem] Coba memilih upgrade null.");
                return;
            }

            PlayerStats target = ResolvePlayerStats();
            if (target == null)
            {
                Debug.LogWarning("[UpgradeSystem] PlayerStats tidak ditemukan di scene, upgrade \"" + choice.DisplayName + "\" tidak diterapkan.");
                return;
            }

            target.ApplyUpgrade(choice.Type, choice.Value);
            OnUpgradeApplied?.Invoke(choice);
        }

        private PlayerStats ResolvePlayerStats()
        {
            if (playerStats == null)
            {
                playerStats = FindFirstObjectByType<PlayerStats>();
            }
            return playerStats;
        }
    }
}