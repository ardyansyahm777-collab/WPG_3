using System;
using UnityEngine;

namespace WpgGame.Core
{
    /// <summary>
    /// Event bus global untuk kejadian yang tidak terikat satu MonoBehaviour.
    /// Pakai ini untuk hal-hal yang murni broadcast (game over, exp gained, gold dropped, enemy killed, dll).
    /// </summary>
    public static class GameEvents
    {
        // --- Kematian & damage ---
        public static event Action<GameObject> OnEnemyKilled;       // arg: GameObject musuh yang mati
        public static event Action<GameObject> OnPlayerDied;        // arg: GameObject player

        // --- Progression ---
        public static event Action<float> OnExperienceGained;      // arg: jumlah EXP
        public static event Action OnLevelUp;                       // no arg; subscriber UI tampilkan popup

        // --- Economy ---
        public static event Action<int> OnGoldChanged;              // arg: total gold sekarang

        // --- Game state ---
        public static event Action OnGameOver;

        // --- Helper untuk raise (agar penamaan konsisten) ---
        public static void RaiseEnemyKilled(GameObject enemy) => OnEnemyKilled?.Invoke(enemy);
        public static void RaisePlayerDied(GameObject player) => OnPlayerDied?.Invoke(player);
        public static void RaiseExperienceGained(float amount) => OnExperienceGained?.Invoke(amount);
        public static void RaiseLevelUp() => OnLevelUp?.Invoke();
        public static void RaiseGoldChanged(int total) => OnGoldChanged?.Invoke(total);
        public static void RaiseGameOver() => OnGameOver?.Invoke();

        // --- Cleanup saat restart/load scene ---
        public static void ClearAll()
        {
            OnEnemyKilled = null;
            OnPlayerDied = null;
            OnExperienceGained = null;
            OnLevelUp = null;
            OnGoldChanged = null;
            OnGameOver = null;
        }
    }
}
