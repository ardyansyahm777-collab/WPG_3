using UnityEngine;
using UnityEngine.InputSystem;
using WpgGame.Core;

namespace WpgGame.Player
{
    /// <summary>
    /// Menggerakkan player (top-down) dari Input Action "Player/Move" (asset bawaan InputSystem_Actions).
    /// Berlaku untuk keyboard/WASD (KeyboardMouse) dan touch joystick (Touch) karena keduanya sudah ter-bind
    /// di asset tsb (jangan buat input action baru). Pergerakan memakai Rigidbody2D.linearVelocity (Unity 6).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Drag action Player/Move dari asset InputSystem_Actions ke sini.")]
        [SerializeField] private InputActionReference moveAction;

        [Tooltip("Referensi Rigidbody2D (otomatis diambil jika kosong).")]
        [SerializeField] private Rigidbody2D rb;

        [Tooltip("Referensi PlayerStats (otomatis diambil jika kosong).")]
        [SerializeField] private PlayerStats stats;

        private bool _warnedMissingAction;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stats = GetComponent<PlayerStats>();

#if UNITY_EDITOR
            // Fallback editor: ambil referensi langsung dari asset bawaan agar Play mode langsung jalan
            // tanpa wiring manual. Untuk build final, reference di-assign lewat inspector oleh integrator.
            if (moveAction == null)
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
                var move = asset != null ? asset.FindAction("Player/Move", true) : null;
                if (move != null) moveAction = InputActionReference.Create(move);
            }
#endif
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            if (rb == null || stats == null) return;

            Vector2 input = Vector2.zero;
            if (moveAction != null)
            {
                input = moveAction.action.ReadValue<Vector2>();
            }
            else if (!_warnedMissingAction)
            {
                _warnedMissingAction = true;
                Debug.LogWarning("[PlayerController] moveAction belum di-assign. Assign action Player/Move dari InputSystem_Actions ke inspector.", this);
            }

            // Cap di 1 untuk diagonal (WASD), tapi tetap mempertahankan analog parsial touch joystick.
            input = Vector2.ClampMagnitude(input, 1f);

            if (IsInputLocked()) input = Vector2.zero;

            rb.linearVelocity = input * stats.MoveSpeed;
        }

        private bool IsInputLocked()
        {
            var gm = GameManager.Instance;
            if (gm == null) return false;
            return gm.State != GameManager.GameState.Playing;
        }
    }
}
