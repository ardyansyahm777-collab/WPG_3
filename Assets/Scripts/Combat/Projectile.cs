using UnityEngine;
using WpgGame.Player;

namespace WpgGame.Combat
{
    /// <summary>
    /// Proyektil sekali-hantan yang diluncurkan AutoAimShooter (player).
    /// Bergerak lurus dengan kecepatan <see cref="Speed"/>, memberi damage lewat <see cref="HealthSystem.TakeDamage"/>
    /// saat menyentuh collider di <see cref="HitMask"/>, lalu menghilang sendiri setelah <see cref="Lifetime"/> detik
    /// atau melewati <see cref="MaxRange"/> dari titik lahir.
    /// Body type Rigidbody2D: Kinematic + Collider2D isTrigger.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [Tooltip("Damage yang diberikan saat mengenai musuh (bisa di-override pemilik proyektil).")]
        public float Damage = 1f;

        [Tooltip("Umur proyektil (detik) sebelum menghilang sendiri.")]
        public float Lifetime = 3f;

        [Tooltip("Kecepatan terbang proyektil (unit/detik).")]
        public float Speed = 10f;

        [Tooltip("Layer mask yang bisa dihantam proyektil (isi dengan layer musuh PG_Enemy).")]
        public LayerMask HitMask;

        [Tooltip("Jarak tempuh maksimal dari titik lahir (0 = tidak terbatas).")]
        public float MaxRange = 0f;

        private Rigidbody2D _rb;
        private Vector2 _startPosition;
        private Vector2 _direction;
        private float _lifeTimer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (HitMask.value == 0)
            {
                // Fallback: layer musuh bawaan hasil bootstrap (PG_Enemy).
                int layer = LayerMask.NameToLayer("PG_Enemy");
                HitMask = layer >= 0 ? 1 << layer : ~0;
            }
        }

        public void Launch(Vector2 direction)
        {
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _startPosition = transform.position;
            _lifeTimer = 0f;
            if (_rb != null)
            {
                _rb.linearVelocity = _direction * Speed;
            }
        }

        private void Update()
        {
            _lifeTimer += Time.deltaTime;
            bool timeout = _lifeTimer >= Lifetime;
            bool outOfRange = MaxRange > 0f && Vector2.Distance(_startPosition, transform.position) > MaxRange;
            if (timeout || outOfRange)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;

            // Jangan hantam player atau proyektil lain.
            if (other.GetComponentInParent<PlayerStats>() != null) return;
            if (other.GetComponent<Projectile>() != null) return;

            int otherLayerBit = 1 << other.gameObject.layer;
            if ((HitMask.value & otherLayerBit) == 0) return;

            var health = other.GetComponentInParent<HealthSystem>();
            if (health == null || health.IsDead) return;

            health.TakeDamage(Damage);
            Destroy(gameObject);
        }
    }
}
