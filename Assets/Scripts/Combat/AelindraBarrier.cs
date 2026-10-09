using UnityEngine;
using WpgGame.Core;
using WpgGame.Enemy;

namespace WpgGame.Combat
{
    /// <summary>
    /// Barier statis Aelindra (Skill2): lingkaran solid di titik cast yang menahan
    /// musuh biasa, dilewati boss (<see cref="EnemyAI.IsBoss"/>).
    /// Dibangun runtime oleh <see cref="Player.AelindraSkill"/> (tanpa file prefab):
    /// Rigidbody2D Kinematic + CircleCollider2D solid + SpriteRenderer ring.
    /// Layer Default (0): bertabrakan dengan PG_Enemy (hanya pasangan
    /// musuh-vs-musuh yang di-ignore) dan diabaikan terhadap player lewat
    /// Physics2D.IgnoreCollision per-pasangan, sehingga player bebas keluar-masuk.
    /// Durasi hanya berkurang saat state Playing (freeze saat popup/pause).
    /// </summary>
    public class AelindraBarrier : MonoBehaviour
    {
        private float _timeLeft;
        private float _duration = 6f;
        private bool _active;
        private CircleCollider2D _col;
        private SpriteRenderer _sr;
        private Transform _visual;
        private Color _baseColor = new Color(0.35f, 1f, 1f, 0.85f);

        /// <summary>Radius dunia yang digambar sprite ring bawaan (visual diskala ke radius aktual).</summary>
        private const float DefaultVisualRadius = 3f;

        private static Sprite _ringSprite;

        /// <summary>
        /// Bangun ulang GameObject barier dari kode (tanpa file prefab):
        /// Kinematic Rigidbody2D + CircleCollider2D solid + child visual ring cyan.
        /// </summary>
        public static AelindraBarrier Build(Vector2 pos, float radius)
        {
            var go = new GameObject("AelindraBarrier");
            go.layer = 0; // Default: tabrakan dengan PG_Enemy, diabaikan vs player per-pasangan.

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = false;
            col.radius = Mathf.Max(0.1f, radius);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateRingSprite();
            sr.color = new Color(0.35f, 1f, 1f, 0.85f);
            sr.sortingOrder = 2;

            go.transform.position = pos;
            RuntimeSpawnTag.Tag(go, "WpgGame.Combat.AelindraBarrier.Build");
            return go.AddComponent<AelindraBarrier>();
        }

        private static Sprite GetOrCreateRingSprite()
        {
            if (_ringSprite != null) return _ringSprite;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float outer = size * 0.5f;
            float inner = outer * 0.84f;
            Color ring = new Color(0.35f, 1f, 1f, 1f);
            Color fill = new Color(0.35f, 1f, 1f, 0.10f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    tex.SetPixel(x, y, d <= outer ? (d >= inner ? ring : fill) : Color.clear);
                }
            }
            tex.Apply();
            // Diameter sprite = 2x DefaultVisualRadius unit dunia.
            _ringSprite = Sprite.Create(
                tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                size / (DefaultVisualRadius * 2f));
            _ringSprite.name = "aelindra_barrier_ring";
            return _ringSprite;
        }

        /// <summary>True selama barier tampil dan berumur.</summary>
        public bool IsActive
        {
            get { return _active && gameObject.activeInHierarchy && _timeLeft > 0f; }
        }

        /// <summary>
        /// Tampilkan/pindah barier ke posisi cast dengan radius &amp; durasi baru.
        /// Collider player diabaikan agar tidak terdorong; boss yang sudah ada
        /// diabaikan duluan, yang datang belakangan lolos via OnCollisionEnter2D.
        /// </summary>
        public void Activate(Vector2 pos, float radius, float duration, Collider2D[] playerColliders)
        {
            transform.SetPositionAndRotation(pos, Quaternion.identity);
            _duration = Mathf.Max(0.1f, duration);
            _timeLeft = _duration;

            if (_col == null) _col = GetComponent<CircleCollider2D>();
            if (_col != null)
            {
                _col.radius = Mathf.Max(0.1f, radius);
                if (playerColliders != null)
                {
                    for (int i = 0; i < playerColliders.Length; i++)
                    {
                        if (playerColliders[i] != null)
                            Physics2D.IgnoreCollision(_col, playerColliders[i], true);
                    }
                }
                PreIgnoreBosses();
            }

            if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null)
            {
                _visual = _sr.transform;
                // Visual ring digambar untuk DefaultVisualRadius → skala ke radius aktual.
                // (Collider di root, skala visual di child, jadi tidak saling mengganggu.)
                _visual.localScale = Vector3.one * (Mathf.Max(0.1f, radius) / DefaultVisualRadius);
                _baseColor = _sr.color;
                _baseColor.a = Mathf.Max(_baseColor.a, 0.5f);
                _sr.color = _baseColor;
            }

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                if (rb.IsSleeping()) rb.WakeUp();
            }

            _active = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_active) return;

            // Freeze: umur tidak berkurang saat bukan Playing.
            var gm = GameManager.Instance;
            if (gm != null && gm.State != GameManager.GameState.Playing) return;

            _timeLeft -= Time.deltaTime;

            // Fade 1 detik terakhir sebagai telegraf habis.
            if (_sr != null && _duration > 0f)
            {
                float a = Mathf.Clamp01(_timeLeft / Mathf.Min(1f, _duration));
                Color c = _baseColor;
                c.a *= a;
                _sr.color = c;
            }

            if (_timeLeft <= 0f)
            {
                _active = false;
                gameObject.SetActive(false);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_active || _col == null || collision == null || collision.collider == null) return;
            var ai = collision.gameObject.GetComponentInParent<EnemyAI>();
            if (ai != null && ai.IsBoss)
            {
                // Boss kebal barier: abaikan tabrakan setelah sentuhan pertama.
                Physics2D.IgnoreCollision(_col, collision.collider, true);
            }
        }

        private void PreIgnoreBosses()
        {
            var all = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var boss = all[i];
                if (boss == null || !boss.IsBoss || !boss.gameObject.activeInHierarchy) continue;
                var cols = boss.GetComponents<Collider2D>();
                for (int c = 0; c < cols.Length; c++)
                {
                    if (cols[c] != null) Physics2D.IgnoreCollision(_col, cols[c], true);
                }
            }
        }
    }
}
