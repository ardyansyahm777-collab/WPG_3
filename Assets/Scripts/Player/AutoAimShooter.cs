using UnityEngine;
using UnityEngine.InputSystem;
using WpgGame.Combat;
using WpgGame.Core;
using WpgGame.Enemy;

namespace WpgGame.Player
{
    /// <summary>
    /// Tembakan otomatis mengarah ke posisi mouse — tanpa perlu klik.
    /// Tiap interval FireRate menembakkan burst MultiShot ke arah kursor terkini.
    /// Tanpa mouse (touch/HP): fallback auto-aim ke musuh terdekat dalam AimRadius;
    /// bila tak ada target, tembakan ditahan (tidak buang proyektil).
    /// Titik tembak: FirePoint bila diisi, else posisi + MuzzleOffset (default tengah badan).
    /// Freeze saat GameManager.State != Playing.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class AutoAimShooter : MonoBehaviour
    {
        [Tooltip("Prefab proyektil (berisi komponen Projectile + collider trigger).")]
        public GameObject ProjectilePrefab;

        [Tooltip("Titik asal tembakan. Kosongkan = posisi + MuzzleOffset.")]
        public Transform FirePoint;

        [Tooltip("Offset titik tembak dari posisi. (0,0) = tepat tengah badan.")]
        public Vector2 MuzzleOffset = Vector2.zero;

        [Tooltip("Radius pencarian musuh terdekat (fallback sentuh saat tak ada mouse).")]
        public float AimRadius = 12f;

        [Tooltip("Sudut sebar antar proyektil (radian) saat MultiShot > 1.")]
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
            // Hangatkan pool proyektil di awal agar tembakan pertama tidak hitch.
            // Hanya saat play: cegah sampah [Pool] masuk ke scene saat dibuka di Editor.
            if (Application.isPlaying && ProjectilePrefab != null)
                PrefabPool.GetOrCreate(ProjectilePrefab, 16, 120);
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
            float interval = 1f / Mathf.Max(0.05f, _stats != null ? _stats.FireRate : 1f);
            _cooldown = Mathf.Min(_cooldown, interval);
        }

        private void Update()
        {
            if (_stats == null) return;
            if (ProjectilePrefab == null) return;

            // Cooldown decay pakai frame-rate-independent deltaTime (bukan fixed).
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;
            if (IsInputLocked()) return;

            Vector2 origin = GetMuzzlePosition();
            Vector2 dir = GetAimDirection(origin);
            if (dir == Vector2.zero) return;

            FireBurst(origin, dir);
            _cooldown = 1f / Mathf.Max(0.05f, _stats.FireRate);
        }

        /// <summary>
        /// Titik tembak: FirePoint bila diisi, else tengah badan + MuzzleOffset.
        /// </summary>
        private Vector2 GetMuzzlePosition()
        {
            if (FirePoint != null) return FirePoint.position;
            return (Vector2)transform.position + MuzzleOffset;
        }

        /// <summary>
        /// Arah tembakan: ke kursor mouse bila ada mouse; else musuh terdekat (sentuh).
        /// Return Vector2.zero bila tak ada arah valid (tahan tembakan).
        /// </summary>
        private Vector2 GetAimDirection(Vector2 origin)
        {
            var mouse = Mouse.current;
            if (mouse != null && Camera.main != null)
            {
                Vector3 mouseScreen = mouse.position.ReadValue();

                // Z yang dipakai ScreenToWorldPoint = jarak camera ke plane tempat player berdiri.
                float zDist = transform.position.z - Camera.main.transform.position.z;

                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
                    new Vector3(mouseScreen.x, mouseScreen.y, zDist));

                Vector2 d = (Vector2)mouseWorld - origin;
                if (d.sqrMagnitude < 0.0001f) return Vector2.zero;
                return d.normalized;
            }

            return GetAutoAimDirection(origin);
        }

        /// <summary>
        /// Fallback sentuh: arah ke musuh terdekat dalam AimRadius (abaikan yang mati/nonaktif).
        /// Hanya dipanggil saat tembakan siap (bukan tiap frame), jadi FindObjects di sini murah.
        /// </summary>
        private Vector2 GetAutoAimDirection(Vector2 origin)
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

                bestSqr = sqr;
                nearest = enemy;
            }

            if (nearest == null) return Vector2.zero;

            Vector2 d = (Vector2)nearest.transform.position - origin;
            if (d.sqrMagnitude < 0.0001f) return Vector2.zero;
            return d.normalized;
        }

        /// <summary>
        /// Satu burst = MultiShot proyektil dengan spread mengarah ke 'direction', lahir di 'origin'.
        /// </summary>
        private void FireBurst(Vector2 origin, Vector2 direction)
        {
            int count = Mathf.Max(1, _stats.MultiShot);
            for (int i = 0; i < count; i++)
            {
                float offset = 0f;
                if (count > 1)
                {
                    offset = (i - (count - 1) * 0.5f) * SpreadRadians;
                }
                Vector2 dir = Rotate(direction, offset);

                var go = PrefabPool.Spawn(ProjectilePrefab, origin, Quaternion.identity);
                if (go == null) continue;

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

        private bool IsInputLocked()
        {
            var gm = GameManager.Instance;
            if (gm == null) return false;
            return gm.State != GameManager.GameState.Playing;
        }
    }
}
