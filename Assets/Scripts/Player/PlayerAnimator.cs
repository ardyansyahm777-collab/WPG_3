using UnityEngine;
using WpgGame.Core;

namespace WpgGame.Player
{
    /// <summary>
    /// Driver visual player: meneruskan kecepatan gerak + status menembak ke
    /// Animator (param Speed float, IsShooting bool — lihat PlayerBow.controller),
    /// dan flipX mengikuti arah-x gerakan (sprite menghadap kanan).
    /// Freeze saat GameManager.State != Playing (animator.speed = 0).
    /// </summary>
    /// <remarks>
    /// Sengaja TIDAK [RequireComponent(Animator)]: Animator + SpriteRenderer
    /// duduk di child "Square" (satu-satunya sumber sprite; clip bind path="").
    /// Kalau Animator diwajibkan di sini, Unity menambahkannya juga di root dan
    /// dua animator berebut menulis sprite.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Tooltip("Jendela pose menembak setelah tembakan terakhir (detik).")]
        [SerializeField] private float shootPoseWindow = 0.25f;

        [Tooltip("Magnitude velocity di bawah ini dianggap diam (masuk state Idle).")]
        [SerializeField] private float moveThreshold = 0.1f;

        private Animator _animator;
        private Rigidbody2D _rb;
        private AutoAimShooter _shooter;
        private SpriteRenderer _visual;

        private void Awake()
        {
            // Animator + SpriteRenderer ada di child visuals (Square), bukan di root.
            _animator = GetComponentInChildren<Animator>(true);
            _rb = GetComponent<Rigidbody2D>();
            _shooter = GetComponent<AutoAimShooter>();
            _visual = GetComponentInChildren<SpriteRenderer>(true);
        }

        private void Update()
        {
            if (_animator == null || _rb == null) return;

            var gm = GameManager.Instance;
            bool frozen = gm != null && gm.State != GameManager.GameState.Playing;
            _animator.speed = frozen ? 0f : 1f;
            if (frozen) return;

            Vector2 v = _rb.linearVelocity;
            _animator.SetFloat("Speed", v.magnitude);
            bool shooting = _shooter != null
                && Time.time - _shooter.LastShotTime < Mathf.Max(0.05f, shootPoseWindow);
            _animator.SetBool("IsShooting", shooting);

            if (_visual != null && Mathf.Abs(v.x) > moveThreshold)
                _visual.flipX = v.x < 0f;
        }
    }
}
