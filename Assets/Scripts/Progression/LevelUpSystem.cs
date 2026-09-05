using System;
using UnityEngine;
using WpgGame.Core;

namespace WpgGame.Progression
{
    /// <summary>
    /// Melacak EXP dan level pemain. Saat level up meng-broadcast GameEvents.RaiseLevelUp()
    /// agar UpgradeSystem/UI merespons. Kontrak API (Bagian 10).
    /// Kurva default: ExpToNextLevel = baseExpPerLevel * level (default 50 * level).
    /// </summary>
    public class LevelUpSystem : MonoBehaviour
    {
        [Header("Curve")]
        [Tooltip("EXP yang dibutuhkan naik level = value ini * CurrentLevel (default 50 * level).")]
        [SerializeField] private float baseExpPerLevel = 50f;

        [Header("State")]
        [SerializeField] private float currentExp;
        [SerializeField] private int currentLevel = 1;

        public float CurrentExp => currentExp;
        public int CurrentLevel => currentLevel;

        /// <summary>EXP yang dibutuhkan untuk naik dari level sekarang.</summary>
        public float ExpToNextLevel => GetExpToNextLevel(currentLevel);

        /// <summary>(newLevel)</summary>
        public event Action<int> OnLevelChanged;

        /// <summary>(current, toNext)</summary>
        public event Action<float, float> OnExpChanged;

        // EXP dari jualan musuh didistribusikan lewat global EventBus.
        private void OnEnable()
        {
            GameEvents.OnExperienceGained += HandleExperienceGained;
        }

        private void OnDisable()
        {
            GameEvents.OnExperienceGained -= HandleExperienceGained;
        }

        private void HandleExperienceGained(float amount)
        {
            AddExperience(amount);
        }

        public float GetExpToNextLevel(int level)
        {
            // Kunci kurva agar tidak pernah 0 / tidak terbagi habis (hindari infinite loop level-up).
            return Mathf.Max(1f, baseExpPerLevel) * Mathf.Max(1, level);
        }

        public void AddExperience(float amount)
        {
            if (amount <= 0f) return;

            currentExp += amount;

            // Bisa naik lebih dari 1 level dalam satu add.
            bool leveledUp = false;
            while (currentExp >= ExpToNextLevel)
            {
                currentExp -= ExpToNextLevel;
                currentLevel++;
                leveledUp = true;
                OnLevelChanged?.Invoke(currentLevel);
                GameEvents.RaiseLevelUp();
            }

            OnExpChanged?.Invoke(currentExp, ExpToNextLevel);

            if (leveledUp)
            {
                Debug.Log($"[LevelUpSystem] Naik ke level {currentLevel} (sisa EXP {currentExp:0.#}/{ExpToNextLevel:0.#})");
            }
        }
    }
}