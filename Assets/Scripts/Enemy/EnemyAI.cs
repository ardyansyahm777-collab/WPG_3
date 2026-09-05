using System;
using UnityEngine;
using WpgGame.Combat;
using WpgGame.Core;
using WpgGame.Player;

namespace WpgGame.Enemy
{
    /// <summary>
    /// AI sederhana musuh: mengejar player (ditemukan lewat komponen PlayerController),
    /// bergerak memakai Rigidbody2D.linearVelocity, dan memberi ContactDamage ke player saat bersentuhan.
    /// Saat HP habis: menaikkan OnEnemyDefeated + broadcast GameEvents.RaiseEnemyKilled lalu menghancurkan diri.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(HealthSystem))]
    public class EnemyAI : MonoBehaviour
    {
        [Tooltip("Kecepatan kejar musuh (unit/detik).")]
        public float MoveSpeed = 2f;

        [Tooltip("Damage yang diberikan ke player saat bersentuhan.")]
        public float ContactDamage = 1f;

        [Tooltip("Cooldown (detik) antar setiap kontak damage.")]
        public float ContactInterval = 0.5f;

        [Tooltip("Referensi HealthSystem musuh (otomatis didapat via GetComponent).")]
        public HealthSystem Health;

        /// <summary>Opsional: dipanggil sekali saat musuh mati.</summary>
        public event Action<EnemyAI> OnEnemyDefeated;

        private Rigidbody2D _rb;
        private Transform _player;
        private float _nextContactTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            Health = GetComponent<HealthSystem>();
            _player = FindAndCachePlayer();
        }

        private void OnEnable()
        {
            if (Health != null) Health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (Health != null) Health.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            OnEnemyDefeated?.Invoke(this);
            GameEvents.RaiseEnemyKilled(gameObject);
            Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (_rb == null || Health == null || Health.IsDead) return;

            if (_player == null)
            {
                _player = FindAndCachePlayer();
                if (_player == null)
                {
                    _rb.linearVelocity = Vector2.zero;
                    return;
                }
            }

            Vector2 diff = (Vector2)(_player.position - transform.position);
            if (diff.sqrMagnitude > 0.01f)
            {
                _rb.linearVelocity = diff.normalized * MoveSpeed;
            }
            else
            {
                _rb.linearVelocity = Vector2.zero;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryContact(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryContact(collision.collider);
        }

        private void TryContact(Collider2D other)
        {
            if (other == null || Time.time < _nextContactTime) return;
            if (other.GetComponentInParent<PlayerStats>() == null) return;

            var playerHealth = other.GetComponentInParent<HealthSystem>();
            if (playerHealth == null || playerHealth.IsDead) return;

            playerHealth.TakeDamage(ContactDamage);
            _nextContactTime = Time.time + Mathf.Max(0.1f, ContactInterval);
        }

        private Transform FindAndCachePlayer()
        {
            var controller = FindFirstObjectByType<PlayerController>();
            return controller != null ? controller.transform : null;
        }
    }
}
