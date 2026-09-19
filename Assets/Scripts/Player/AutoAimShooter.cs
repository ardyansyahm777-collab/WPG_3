using UnityEngine;
using UnityEngine.InputSystem;
using WpgGame.Combat;
using WpgGame.Core;

namespace WpgGame.Player
{
    /// <summary>
    /// Tembakan dasar (basic attack) dengan trigger klik kiri mouse.
    /// Satu klik = burst MultiShot proyektil mengarah ke posisi mouse.
    /// FireRate dipakai sebagai cooldown anti-spam per burst (bukan auto-fire kontinu).
    /// Freeze saat GameManager.State != Playing.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class AutoAimShooter : MonoBehaviour
    {
        [Tooltip("Prefab proyektil (berisi komponen Projectile + collider trigger).")]
        public GameObject ProjectilePrefab;

        [Tooltip("Titik asal tembakan. Kosongkan = gunakan transform komponen ini.")]
        public Transform FirePoint;

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

            // Trigger: klik kiri mouse.
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (!mouse.leftButton.wasPressedThisFrame) return;

            Vector2 dir = GetMouseDirection();
            if (dir == Vector2.zero) return;

            FireBurst(dir);
            _cooldown = 1f / Mathf.Max(0.05f, _stats.FireRate);
        }

        /// <summary>
        /// Arah tembakan: dari fire point ke posisi mouse (screen → world).
        /// Return Vector2.zero jika mouse tidak tersedia / kamera tidak ada / posisi sama.
        /// </summary>
        private Vector2 GetMouseDirection()
        {
            if (Camera.main == null) return Vector2.zero;

            var mouse = Mouse.current;
            if (mouse == null) return Vector2.zero;

            Vector3 mouseScreen = mouse.position.ReadValue();

            // Z yang dipakai ScreenToWorldPoint = jarak camera ke plane tempat player berdiri.
            float zDist = FirePoint != null
                ? FirePoint.position.z - Camera.main.transform.position.z
                : -Camera.main.transform.position.z;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
                new Vector3(mouseScreen.x, mouseScreen.y, zDist));

            Vector2 origin = FirePoint != null ? FirePoint.position : transform.position;
            Vector2 dir = (Vector2)mouseWorld - origin;
            if (dir.sqrMagnitude < 0.0001f) return Vector2.zero;
            return dir.normalized;
        }

        /// <summary>
        /// Satu burst = MultiShot proyektil dengan spread mengarah ke 'direction'.
        /// </summary>
        private void FireBurst(Vector2 direction)
        {
            Vector2 origin = FirePoint != null ? FirePoint.position : transform.position;

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
