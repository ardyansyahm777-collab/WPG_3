using System.Collections;
using UnityEngine;
using WpgGame.Core;
using WpgGame.Player;

namespace WpgGame.Enemy
{
    /// <summary>
    /// Spawn musuh periodik di sekitar player dalam "wave".
    /// Config: SpawnInterval (jeda antar wave), WaveSize (jumlah musuh per wave), EnemyPrefab.
    /// Menghormati state GameManager: berhenti spawn saat bukan Playing (pause/level-up/game over).
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Tooltip("Prefab musuh (berisi EnemyAI + HealthSystem).")]
        public GameObject EnemyPrefab;

        [Tooltip("Jeda antar wave (detik).")]
        public float SpawnInterval = 2f;

        [Tooltip("Jumlah musuh yang muncul per wave.")]
        public int WaveSize = 1;

        [Tooltip("Titik referensi spawn (pemain). Kosongkan = cari player otomatis.")]
        public Transform Player;

        [Tooltip("Jarak minimum spawn dari titik referensi.")]
        public float SpawnRadiusMin = 6f;

        [Tooltip("Jarak maksimum spawn dari titik referensi.")]
        public float SpawnRadiusMax = 9f;

        [Tooltip("Mulat spawn langsung saat object aktif.")]
        public bool AutoStart = true;

        /// <summary>Dipanggil setelah satu wave ter-spawn, arg = jumlah musuh yang muncul.</summary>
        public event System.Action<int> OnWaveSpawned;

        private Coroutine _loopRoutine;
        private bool _spawning;

        private void OnEnable()
        {
            if (AutoStart) StartSpawning();
        }

        private void OnDisable()
        {
            StopSpawning();
        }

        public void StartSpawning()
        {
            if (_spawning) return;
            _spawning = true;
            if (_loopRoutine == null) _loopRoutine = StartCoroutine(SpawnLoop());
        }

        public void StopSpawning()
        {
            _spawning = false;
            if (_loopRoutine != null)
            {
                StopCoroutine(_loopRoutine);
                _loopRoutine = null;
            }
        }

        private IEnumerator SpawnLoop()
        {
            while (_spawning)
            {
                if (CanSpawnNow()) SpawnWave();
                yield return new WaitForSeconds(Mathf.Max(0.05f, SpawnInterval));
            }
            _loopRoutine = null;
        }

        private bool CanSpawnNow()
        {
            var gm = GameManager.Instance;
            return gm == null || gm.State == GameManager.GameState.Playing;
        }

        private void SpawnWave()
        {
            if (EnemyPrefab == null) return;

            var origin = ResolveSpawnOrigin();
            int n = Mathf.Max(0, WaveSize);
            if (n == 0) return;

            for (int i = 0; i < n; i++)
            {
                Vector2 basePos = origin != null ? (Vector2)origin.position : Vector2.zero;
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(SpawnRadiusMin, SpawnRadiusMax);
                Vector2 pos = basePos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                var instance = Instantiate(EnemyPrefab, pos, Quaternion.identity);
                if (instance != null && !instance.activeSelf) instance.SetActive(true);
            }

            OnWaveSpawned?.Invoke(n);
        }

        private Transform ResolveSpawnOrigin()
        {
            if (Player != null) return Player;
            var controller = FindFirstObjectByType<PlayerController>();
            if (controller != null) Player = controller.transform;
            return Player;
        }
    }
}
