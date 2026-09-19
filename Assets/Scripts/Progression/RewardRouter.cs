using UnityEngine;
using WpgGame.Core;
using WpgGame.Economy;

namespace WpgGame.Progression
{
    /// <summary>
    /// FASE 1: subscribe OnEnemyKilled, spawn drop ExpGem + GoldCoin di posisi enemy mati.
    /// Komunikasi murni via GameEvents; tidak menyentuh sistem lain langsung.
    /// </summary>
    public class RewardRouter : MonoBehaviour
    {
        [SerializeField] private float expPerKill = 10f;
        [SerializeField] private int goldPerKill = 5;

        [Tooltip("Batas pickup (gem+koin) yang hidup bersamaan. Kelebihan TIDAK dibuang: nilainya " +
                 "digabung ke pickup terdekat (ekonomi tetap sama, jumlah objek bounded → CPU stabil).")]
        [SerializeField] private int maxLivePickups = 60;

        private static GameObject _gemTemplate;
        private static GameObject _coinTemplate;

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            // Hangatkan pool drop agar kill pertama tidak hitch (template dibuat sekali, Awake jalan sekali).
            // Hanya saat play: cegah sampah [Pool] masuk ke scene saat dibuka di Editor.
            if (Application.isPlaying)
            {
                PrefabPool.GetOrCreate(GemTemplate(), 10, 100);
                PrefabPool.GetOrCreate(CoinTemplate(), 10, 100);
            }
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(GameObject enemy)
        {
            if (enemy == null)
                return;

            Vector3 basePos = enemy.transform.position;

            // Anti-penumpukan: bila pickup sudah banyak (player lari terus tanpa memungut),
            // gabung nilai ke pickup terdekat alih-alih bikin objek baru terus.
            if (CountLivePickups() >= Mathf.Max(1, maxLivePickups))
            {
                MergeIntoNearest(basePos);
                return;
            }

            SpawnExpGem(basePos);
            SpawnGoldCoin(basePos);
        }

        private void SpawnExpGem(Vector3 basePos)
        {
            // Via pool: tanpa new GameObject + AddComponent per kill (dulu sumber GC spike).
            var go = PrefabPool.Spawn(GemTemplate(),
                basePos + (Vector3)Random.insideUnitCircle * 0.3f, Quaternion.identity);
            if (go == null) return;
            var gem = go.GetComponent<ExpGem>();
            if (gem != null) gem.ExpAmount = expPerKill;
        }

        private void SpawnGoldCoin(Vector3 basePos)
        {
            var go = PrefabPool.Spawn(CoinTemplate(),
                basePos + (Vector3)Random.insideUnitCircle * 0.3f, Quaternion.identity);
            if (go == null) return;
            var coin = go.GetComponent<GoldCoin>();
            if (coin != null) coin.GoldAmount = goldPerKill;
        }

        /// <summary>
        /// Template drop dibuat sekali (Awake komponen jalan sekali via prewarm pool),
        /// lalu dipakai ulang. Validasi null karena static basi setelah reload scene.
        /// </summary>
        private static GameObject GemTemplate()
        {
            if (_gemTemplate == null)
            {
                _gemTemplate = new GameObject("ExpGemTemplate", typeof(ExpGem));
                _gemTemplate.SetActive(false);
            }
            return _gemTemplate;
        }

        private static GameObject CoinTemplate()
        {
            if (_coinTemplate == null)
            {
                _coinTemplate = new GameObject("GoldCoinTemplate", typeof(GoldCoin));
                _coinTemplate.SetActive(false);
            }
            return _coinTemplate;
        }

        /// <summary>
        /// Hitung pickup hidup. Hanya dipanggil saat ada kill (jarang), bukan per-frame.
        /// </summary>
        private static int CountLivePickups()
        {
            int gems = FindObjectsByType<ExpGem>(FindObjectsSortMode.None).Length;
            int coins = FindObjectsByType<GoldCoin>(FindObjectsSortMode.None).Length;
            return gems + coins;
        }

        /// <summary>
        /// Gabung nilai kill ke pickup sejenis terdekat. Ekonomi identik dengan spawn baru,
        /// tapi jumlah GameObject tidak bertambah.
        /// </summary>
        private void MergeIntoNearest(Vector3 basePos)
        {
            var gem = FindNearest<ExpGem>(basePos);
            if (gem != null) gem.ExpAmount += expPerKill;
            else SpawnExpGem(basePos);

            var coin = FindNearest<GoldCoin>(basePos);
            if (coin != null) coin.GoldAmount += goldPerKill;
            else SpawnGoldCoin(basePos);
        }

        private static T FindNearest<T>(Vector3 basePos) where T : MonoBehaviour
        {
            var all = FindObjectsByType<T>(FindObjectsSortMode.None);
            T best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                float sqr = (t.transform.position - basePos).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = t;
                }
            }
            return best;
        }
    }
}
