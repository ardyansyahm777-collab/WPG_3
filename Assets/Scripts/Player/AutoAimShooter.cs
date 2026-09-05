using UnityEngine;
using WpgGame.Combat;
using WpgGame.Core;
using WpgGame.Enemy;

namespace WpgGame.Player
{
    /// <summary>
    /// Pencari target otomatis: menembak musuh terdekat dalam radius secara periodik sesuai PlayerStats.FireRate.
    /// Damage & MultiShot ikut PlayerStats. Subscribe PlayerStats.OnStatsChanged untuk re-kalkulasi fire rate.
    /// Jika ProjectilePrefab kosong, otomatis memakai template placeholder dari GameplaySetup.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class AutoAimShooter : MonoBehaviour
    {
        [Tooltip("Prefab proyektil (berisi komponen Projectile + collider trigger).")]
        public GameObject ProjectilePrefab;

        [Tooltip("Titik asal tembakan. Kosongkan = gunakan transform komponen ini.")]
        public Transform FirePoint;

        [Tooltip("Radius pencarian musuh terdekat.")]
        public float AimRadius = 12f;

        [Tooltip("Jarak minimal dari musuh agar tetap ditembak.")]
        public float MinAimDistance = 0.5f;

        [Tooltip("Sudut sebar antar proyektil (radian) saat MultiShot &gt; 1.")]
        public float SpreadRadians = 0.15f;

        [Tooltip("Jika true dan ProjectilePrefab kosong, buat template placeholder otomatis saat Awake.")]
        public bool AutoCreateProjectile = true;

        private PlayerStats _stats;
        private float _cooldown;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            if (ProjectilePrefab == null && AutoCreateProjectile)
            {
                ProjectilePrefab = GameplaySetup.GetOrCreateProjectileTemplate();
            }
        }

        private void OnEnable()
        {
            if (_stats != null) _stats.OnStatsChanged += HandleStatsChanged;
        }

        private void OnDisable()
        {
            if (_stats != null) _stats.OnStatsChanged -= HandleStatsChanged;
        }

        private void HandleStatsChanged(PlayerStats stats)
        {
            // Fire rate baru boleh lebih cepat: jangan biarkan cooldown lama menahan tembakan.
            float interval = 1f / Mathf.Max(0.05f, _stats != null ? _stats.FireRate : 1f);
            _cooldown = Mathf.Min(_cooldown, interval);
        }

        private void FixedUpdate()
        {
            if (_stats == null) return;
            if (ProjectilePrefab == null) return;

            _cooldown -= Time.fixedDeltaTime;
            if (_cooldown > 0f) return;
            if (IsInputLocked()) return;

            var target = FindNearestEnemy();
            if (target == null)
            {
                _cooldown = 0.05f;
                return;
            }

            FireAt(target.transform.position);
            _cooldown = 1f / Mathf.Max(0.05f, _stats.FireRate);
        }

        private bool IsInputLocked()
        {
            var gm = GameManager.Instance;
            if (gm == null) return false;
            return gm.State != GameManager.GameState.Playing;
        }

        private EnemyAI FindNearestEnemy()
        {
            EnemyAI nearest = null;
            float bestSqr = AimRadius * AimRadius;
            var enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                if (enemy.Health != null && enemy.Health.IsDead) continue;

                float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
                if (sqr > bestSqr) continue;
                if (sqr < MinAimDistance * MinAimDistance) continue;

                bestSqr = sqr;
                nearest = enemy;
            }

            return nearest;
        }

        private void FireAt(Vector2 targetPosition)
        {
            Vector2 origin = FirePoint != null ? (Vector2)FirePoint.position : (Vector2)transform.position;
            Vector2 baseDir = targetPosition - origin;
            if (baseDir.sqrMagnitude < 0.0001f) return;
            baseDir.Normalize();

            int count = Mathf.Max(1, _stats.MultiShot);
            for (int i = 0; i < count; i++)
            {
                float offset = 0f;
                if (count > 1)
                {
                    offset = (i - (count - 1) * 0.5f) * SpreadRadians;
                }
                Vector2 dir = Rotate(baseDir, offset);

                var go = Instantiate(ProjectilePrefab, origin, Quaternion.identity);
                if (go == null) continue;
                if (!go.activeSelf) go.SetActive(true); // template placeholder sengaja disimpan inactive

                var projectile = go.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Damage = _stats.Damage;
                    projectile.Launch(dir);
                }
            }
        }

        private static Vector2 Rotate(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
