using System.Collections.Generic;
using UnityEngine;
using WpgGame.Player;

namespace WpgGame.Combat
{
    /// <summary>
    /// Manager pool popup damage (scene-bound, TANPA DontDestroyOnLoad sehingga
    /// restart/reload scene otomatis bersih — pola sama seperti PrefabPool).
    /// Dipastikan ada sekali per scene Main lewat
    /// <c>WpgGame.Player.GameplaySetup.OnSceneLoaded</c> (memanggil Ensure).
    /// Sumber popup: event detail proyektil/chain (musuh) + OnDamaged milik
    /// HealthSystem player (merah). Subscribe di OnEnable, lepas di OnDisable.
    /// </summary>
    public class DamagePopupManager : MonoBehaviour
    {
        [Tooltip("Instance siap di pool saat awal (hindari hitch).")]
        public int PrewarmCount = 20;

        [Tooltip("Maks popup aktif bersamaan; kelebihan memakai ulang yang tertua.")]
        public int MaxActive = 32;

        [Tooltip("Usia popup (detik) sebelum kembali ke pool.")]
        public float PopupLife = 0.7f;

        [Tooltip("Kecepatan naik popup (unit/detik).")]
        public float RiseSpeed = 1.1f;

        [Tooltip("Jitter posisi acak (±unit) agar hit beruntun tidak menumpuk.")]
        public float SpawnJitter = 0.25f;

        [Tooltip("Ukuran font world-space: hit normal.")]
        public float NormalFontSize = 7f;

        [Tooltip("Ukuran font world-space: crit.")]
        public float CritFontSize = 10f;

        [Tooltip("Ukuran font world-space: damage ke player.")]
        public float PlayerFontSize = 8f;

        private static DamagePopupManager _instance;

        private readonly Queue<DamagePopup> _free = new Queue<DamagePopup>();
        private readonly List<DamagePopup> _active = new List<DamagePopup>();
        private HealthSystem _playerHealth;
        private float _bindRetryTimer;

        /// <summary>Ambil/buat satu-satunya manager di scene aktif.</summary>
        public static DamagePopupManager Ensure()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("[DamagePopups]");
            Core.RuntimeSpawnTag.Tag(go, "WpgGame.Combat.DamagePopupManager.Ensure");
            _instance = go.AddComponent<DamagePopupManager>();
            _instance.Prewarm();
            return _instance;
        }

        private void Prewarm()
        {
            for (int i = 0; i < Mathf.Max(0, PrewarmCount); i++)
                _free.Enqueue(CreateInstance());
        }

        private DamagePopup CreateInstance()
        {
            var go = new GameObject("DamagePopup");
            go.transform.SetParent(transform, false);
            var popup = go.AddComponent<DamagePopup>();
            popup.Init(this);
            Core.RuntimeSpawnTag.Tag(go, "WpgGame.Combat.DamagePopupManager.CreateInstance");
            return popup;
        }

        private void OnEnable()
        {
            Projectile.OnProjectileHitDetailed += HandleEnemyDamaged;
            TryBindPlayer();
        }

        private void OnDisable()
        {
            Projectile.OnProjectileHitDetailed -= HandleEnemyDamaged;
            UnbindPlayer();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            // Player dibuat runtime oleh GameplaySetup; coba ulang bind sampai ketemu.
            if (_playerHealth != null) return;
            _bindRetryTimer -= Time.deltaTime;
            if (_bindRetryTimer > 0f) return;
            _bindRetryTimer = 0.5f;
            TryBindPlayer();
        }

        private void TryBindPlayer()
        {
            var stats = FindFirstObjectByType<PlayerStats>();
            if (stats == null) return;
            var health = stats.GetComponent<HealthSystem>();
            if (health == null) return;
            _playerHealth = health;
            _playerHealth.OnDamaged += HandlePlayerDamaged;
        }

        private void UnbindPlayer()
        {
            if (_playerHealth != null) _playerHealth.OnDamaged -= HandlePlayerDamaged;
            _playerHealth = null;
        }

        private void HandleEnemyDamaged(GameObject victim, float damage, bool isCrit)
        {
            if (victim == null || damage <= 0f) return;
            // Abaikan damage ke player di jalur ini (punya jalur merah sendiri).
            if (victim.GetComponentInParent<PlayerStats>() != null) return;
            Vector3 pos = victim.transform.position + RandomOffset();
            Show(pos, damage, isCrit ? DamagePopupKind.Crit : DamagePopupKind.Normal);
        }

        private void HandlePlayerDamaged(float amount)
        {
            if (_playerHealth == null || amount <= 0f) return;
            Vector3 pos = _playerHealth.transform.position + Vector3.up * 0.6f + RandomOffset();
            Show(pos, amount, DamagePopupKind.Player);
        }

        private Vector3 RandomOffset()
        {
            if (SpawnJitter <= 0f) return Vector3.zero;
            return new Vector3(
                UnityEngine.Random.Range(-SpawnJitter, SpawnJitter),
                UnityEngine.Random.Range(-SpawnJitter, SpawnJitter),
                0f);
        }

        private void Show(Vector3 pos, float amount, DamagePopupKind kind)
        {
            DamagePopup popup = null;
            while (_free.Count > 0)
            {
                var candidate = _free.Dequeue();
                if (candidate != null)
                {
                    popup = candidate;
                    break;
                }
            }
            if (popup == null)
            {
                if (_active.Count >= Mathf.Max(1, MaxActive))
                {
                    // Pool panas: pakai ulang popup tertua (index 0 = paling lama).
                    popup = _active[0];
                    _active.RemoveAt(0);
                }
                else
                {
                    popup = CreateInstance();
                }
            }
            _active.Add(popup);
            popup.Show(pos, amount, kind,
                NormalFontSize, CritFontSize, PlayerFontSize, PopupLife, RiseSpeed);
        }

        /// <summary>Kembalikan popup ke pool (dipanggil DamagePopup saat usianya habis).</summary>
        public void Release(DamagePopup popup)
        {
            if (popup == null) return;
            _active.Remove(popup);
            popup.gameObject.SetActive(false);
            popup.transform.SetParent(transform, false);
            _free.Enqueue(popup);
        }
    }
}
