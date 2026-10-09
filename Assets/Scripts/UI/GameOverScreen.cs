using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using WpgGame.Core;

namespace WpgGame.UI
{
    /// <summary>
    /// Layar game-over + victory: tampil saat GameEvents.OnGameOver (kalah) atau
    /// GameEvents.OnVictory (menang). Perilaku OnGameOver existing TIDAK berubah
    /// (judul GAME OVER, tombol Restart + Main Menu reuse). Mode menang aditif:
    /// judul VICTORY + statistik run (waktu, kill). Kill dihitung sendiri via
    /// OnEnemyKilled (tidak ada sistem kill global), reset per run.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private TMP_Text titleText;

        [Header("Victory (aditif)")]
        [Tooltip("Statistik run saat menang: waktu + kill. Disembunyikan saat kalah.")]
        [SerializeField] private TMP_Text statsText;

        private int enemyKillCount;

        private void Awake()
        {
            if (titleText != null && string.IsNullOrEmpty(titleText.text))
            {
                titleText.text = "GAME OVER";
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void OnEnable()
        {
            enemyKillCount = 0;
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnVictory += HandleVictory;
            GameEvents.OnEnemyKilled += HandleEnemyKilled;

            if (panel != null)
            {
                panel.SetActive(false);
            }

            if (statsText != null)
            {
                statsText.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnVictory -= HandleVictory;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(GameObject enemy)
        {
            enemyKillCount++;
        }

        private void HandleGameOver()
        {
            if (titleText != null)
            {
                titleText.text = "GAME OVER";
            }

            if (statsText != null)
            {
                statsText.gameObject.SetActive(false);
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (restartButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(restartButton.gameObject);
            }
        }

        private void HandleVictory()
        {
            if (titleText != null)
            {
                titleText.text = "VICTORY";
            }

            if (statsText != null)
            {
                statsText.text = "Waktu: " + ResolveRunTime() + "\nKill: " + enemyKillCount;
                statsText.gameObject.SetActive(true);
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (restartButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(restartButton.gameObject);
            }
        }

        private static string ResolveRunTime()
        {
            var run = FindFirstObjectByType<RunStats>();
            float elapsed = run != null ? run.ElapsedSeconds : 0f;
            int total = Mathf.Max(0, Mathf.RoundToInt(elapsed));
            int m = total / 60;
            int s = total % 60;
            return m + ":" + s.ToString("00");
        }

        public void OnRestartClicked()
        {
            GameManager.Instance?.Restart();
        }

        /// <summary>Tombol MainmenuButton: kembali ke scene Main Menu (reset bersih).</summary>
        public void OnMainMenuClicked()
        {
            GameManager.Instance?.GoToMainMenu();
        }
    }
}
