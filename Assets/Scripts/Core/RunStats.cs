using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WpgGame.Core
{
    /// <summary>
    /// Run timer: kemenangan saat countdown mencapai 0 (Workstream C).
    /// Tick HANYA saat GameManager.State == Playing (freeze otomatis saat
    /// Paused/LevelUp/GameOver/Victory). Tidak menyentuh Time.timeScale.
    /// Alur finish: GameManager.SetState(Victory) lalu GameEvents.RaiseVictory(),
    /// tepat sekali (guard). SetState(Victory) TIDAK me-raise OnGameOver
    /// (lihat GameManager.SetState) sehingga layar game-over lama tidak muncul.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class RunStats : MonoBehaviour
    {
        public const float DefaultDuration = 300f;

        [Tooltip("Durasi run (detik). Bisa di-tweak inspector. Reset ke nilai ini saat OnEnable.")]
        [SerializeField] private float runDuration = DefaultDuration;

        private float remaining;
        private bool finished;
        private int lastWholeSecond;

        /// <summary>Sisa waktu (detik).</summary>
        public float RemainingSeconds => remaining;

        /// <summary>Durasi run yang dipakai instance ini (detik).</summary>
        public float RunDuration => runDuration;

        /// <summary>Waktu berjalan sejak run mulai (detik).</summary>
        public float ElapsedSeconds => Mathf.Max(0f, runDuration - remaining);

        /// <summary>True setelah timer mencapai 0 dan Victory di-broadcast.</summary>
        public bool IsFinished => finished;

        /// <summary>(remainingSeconds). Di-invoke hanya saat detik tampilan berubah, bukan tiap frame.</summary>
        public event Action<float> OnTimerChanged;

        // Runtime bootstrap (pola GameplaySetup/SceneSetup): pastikan scene Main
        // punya RunStats di object [Systems] (reuse bila sudah ada, buat bila belum).
        // Tidak hand-edit YAML scene. Instance per-scene (tanpa DontDestroyOnLoad)
        // sehingga restart = reload scene = fresh otomatis.
        private static bool _sceneHookInstalled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!_sceneHookInstalled)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                _sceneHookInstalled = true;
            }
            var active = SceneManager.GetActiveScene();
            if (active.IsValid()) OnSceneLoaded(active, LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Main") return;
            EnsureExists();
        }

        /// <summary>
        /// Cari RunStats (termasuk inactive); bila tidak ada, reuse/[buat] [Systems] lalu tambah.
        /// Aman dipanggil berulang. Tanpa pencarian per-frame (hanya saat load scene).
        /// </summary>
        public static RunStats EnsureExists()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<RunStats>(FindObjectsInactive.Include);
            if (existing != null) return existing;

            var systems = GameObject.Find("[Systems]");
            GameObject target = systems != null ? systems : new GameObject("[Systems]");
            if (systems == null)
            {
                RuntimeSpawnTag.Tag(target, "WpgGame.Core.RunStats.EnsureExists");
            }
            var created = target.GetComponent<RunStats>();
            if (created == null) created = target.AddComponent<RunStats>();
            return created;
        }

        private void OnEnable()
        {
            // Restart = reload scene = instance baru; reset eksplisit agar tidak ada
            // sisa state bila object di-reuse (mis. SetActive cycle). Tanpa static
            // state antar run: semua field di sini instance-level.
            remaining = Mathf.Max(1f, runDuration);
            finished = false;
            lastWholeSecond = Mathf.CeilToInt(remaining);
            OnTimerChanged?.Invoke(remaining);
        }

        private void OnDisable()
        {
            // Konvensi subscribe: tidak ada subscription eksternal di sini
            // (polling GameManager.State di Update, tanpa Find per-frame).
            // OnTimerChanged dibiarkan (subscriber membersihkan diri via OnDisable masing-masing).
        }

        private void Update()
        {
            if (finished) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.State != GameManager.GameState.Playing) return;

            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                remaining = 0f;
                if (lastWholeSecond != 0)
                {
                    lastWholeSecond = 0;
                    OnTimerChanged?.Invoke(remaining);
                }
                // Guard tepat-sekali: latch finished HANYA bila SetState benar-benar
                // pindah ke Victory. SetState punya cooldown 0.1 dtk yang bisa menolak
                // panggilan bila state baru saja berubah (timer freeze di LevelUp, lalu
                // return ke Playing di frame yang sama dengan tick) — tanpa cek ini,
                // Victory bisa hilang selamanya. Retry frame berikut bila ditolak.
                gm.SetState(GameManager.GameState.Victory);
                if (gm.State == GameManager.GameState.Victory)
                {
                    finished = true;
                    GameEvents.RaiseVictory();
                }
                return;
            }

            int whole = Mathf.CeilToInt(remaining);
            if (whole != lastWholeSecond)
            {
                lastWholeSecond = whole;
                OnTimerChanged?.Invoke(remaining);
            }
        }
    }
}
