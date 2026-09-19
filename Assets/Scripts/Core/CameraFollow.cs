using UnityEngine;
using WpgGame.Player;

namespace WpgGame.Core
{
    /// <summary>
    /// Kamera mengikuti player (top-down) dengan smoothing.
    /// Target auto-discari saat null (tag Player, fallback PlayerController) agar tahan
    /// terhadap runtime bootstrap (GameplaySetup) dan Restart (reload scene).
    /// Sengaja TIDAK freeze oleh GameManager.State — kamera boleh tetap follow saat
    /// LevelUp/Paused/GameOver agar background tidak lompat.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("Target yang diikuti. Kosongkan = otomatis cari Player saat play.")]
        public Transform Target;

        [Tooltip("Offset dari target. Default (0,0,-10) untuk 2D orthographic.")]
        public Vector3 Offset = new Vector3(0f, 0f, -10f);

        [Tooltip("Waktu smoothing (detik). 0 = snap langsung tanpa smoothing.")]
        [Min(0f)] public float SmoothTime = 0.15f;

        [Tooltip("Jika true, kamera langsung snap ke target saat pertama kali target ketemu.")]
        public bool SnapOnStart = true;

        private Vector3 _velocity;
        private bool _snapped;

        private void OnEnable()
        {
            if (Target == null) TryFindPlayer();
            _snapped = false;
        }

        private void LateUpdate()
        {
            if (Target == null)
            {
                if (!TryFindPlayer()) return;
            }

            Vector3 desired = Target.position + Offset;
            // Paksa z ke Offset.z + target.z? Untuk 2D jaga depth kamera tetap:
            desired.z = Target.position.z + Offset.z;

            if (SnapOnStart && !_snapped)
            {
                transform.position = desired;
                _velocity = Vector3.zero;
                _snapped = true;
                return;
            }

            if (SmoothTime <= 0f)
            {
                transform.position = desired;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, SmoothTime);
            }
        }

        /// <summary>
        /// Assign manual dari bootstrap (GameplaySetup) atau inspector.
        /// </summary>
        public void SetTarget(Transform target)
        {
            Target = target;
            _snapped = false;
        }

        private bool TryFindPlayer()
        {
            GameObject go = null;
            try { go = GameObject.FindWithTag("Player"); } catch { go = null; }
            if (go != null)
            {
                Target = go.transform;
                return true;
            }
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                Target = pc.transform;
                return true;
            }
            return false;
        }
    }
}
