using System;
using UnityEngine;

namespace WpgGame.Core
{
    /// <summary>
    /// Singleton global yang mengatur state utama game.
    /// Dipakai oleh semua sistem lain untuk pause/resume, restart, dan broadcast event state.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            Playing,
            Paused,
            LevelUp,
            GameOver
        }

        [Tooltip("State awal saat game mulai.")]
        [SerializeField] private GameState initialState = GameState.Playing;

        [Tooltip("Delay (detik) sebelum input diizinkan setelah state berubah. Menghindari double-trigger.")]
        [SerializeField] private float stateTransitionCooldown = 0.1f;

        public GameState State { get; private set; }

        /// <summary>Fires (previous, current) setiap state berubah. Subscriber: HUD, spawner, UI popup, dll.</summary>
        public static event Action<GameState, GameState> OnStateChanged;

        private float _lastStateChangeTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            State = initialState;

            // Batasi FPS: tanpa ini Unity me-render secepat mungkin (ratusan FPS di scene
            // ringan) sehingga CPU/GPU 100% → kipas meraung, di laptop maupun HP.
            // 60 hemat baterai dan cukup untuk top-down 2D. Berlaku untuk Editor Play + build.
            if (Application.targetFrameRate != 60)
                Application.targetFrameRate = 60;
        }

        public void SetState(GameState newState)
        {
            if (newState == State) return;
            if (Time.unscaledTime - _lastStateChangeTime < stateTransitionCooldown) return;

            var previous = State;
            State = newState;
            _lastStateChangeTime = Time.unscaledTime;

            // Jika game over, broadcast juga event khusus agar UI/score system bisa respond.
            if (newState == GameState.GameOver)
            {
                GameEvents.RaiseGameOver();
            }
            else if (newState == GameState.Playing && previous == GameState.LevelUp)
            {
                // Saat kembali ke Playing dari LevelUp, anggap upgrade sudah dipilih.
            }

            OnStateChanged?.Invoke(previous, newState);
        }

        public void Restart()
        {
            PrepareSceneLoad();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
        }

        /// <summary>
        /// Kembali ke Main Menu: reset bersih seperti Restart lalu load scene menu.
        /// Dipakai tombol MainmenuButton di GameOverScreen.
        /// </summary>
        public void GoToMainMenu()
        {
            PrepareSceneLoad();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
        }

        /// <summary>
        /// Reset bersih sebelum pindah scene: buang subscriber statis scene lama agar tidak basi,
        /// reset state langsung (bypass cooldown). Instance DontDestroyOnLoad selamat dari
        /// reload, jadi reset eksplisit wajib.
        /// </summary>
        private void PrepareSceneLoad()
        {
            GameEvents.ClearAll();
            OnStateChanged = null;
            State = GameState.Playing;
            _lastStateChangeTime = Time.unscaledTime;
        }
    }
}
