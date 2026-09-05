using System;
using UnityEngine;
using WpgGame.Core;

namespace WpgGame.Economy
{
    /// <summary>
    /// Manager emas (singleton-friendly). Setiap perubahan saldo di-broadcast
    /// lewat event lokal dan global GameEvents.RaiseGoldChanged. Kontrak API (Bagian 11).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GoldManager : MonoBehaviour
    {
        public static GoldManager Instance { get; private set; }

        [SerializeField, Min(0)] private int currentGold;

        public int CurrentGold => currentGold;

        /// <summary>(total gold sekarang)</summary>
        public event Action<int> OnGoldChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Add(int amount)
        {
            if (amount == 0) return;

            currentGold += amount;
            if (currentGold < 0) currentGold = 0;

            GameEvents.RaiseGoldChanged(currentGold);
            OnGoldChanged?.Invoke(currentGold);
        }

        /// <summary>Spend gold jika mencukupi, return false jika saldo kurang.</summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0 || currentGold < amount) return false;

            currentGold -= amount;

            GameEvents.RaiseGoldChanged(currentGold);
            OnGoldChanged?.Invoke(currentGold);
            return true;
        }
    }
}