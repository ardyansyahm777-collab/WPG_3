using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using WpgGame.Combat;
using WpgGame.Core;

namespace WpgGame.Player
{
    /// <summary>
    /// Tiga skill aktif hero: Skill1 (Damage x1.5 selama 3 dtk, CD 8),
    /// Skill2 (MultiShot +1 selama 5 dtk, CD 12), Skill3 (heal instan, CD 20).
    /// Cooldown &amp; nilai heal diambil dari <see cref="Character"/> bila diisi,
    /// jika tidak pakai default di atas. Buff sementara dikembalikan via coroutine
    /// dan dibatalkan saat komponen di-disable. Tidak menyentuh Rigidbody maupun Projectile.
    /// Input dari action Player/Skill1..Skill3 di asset bawaan InputSystem_Actions.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(HealthSystem))]
    public class HeroSkill : MonoBehaviour
    {
        [Tooltip("Drag action Player/Skill1 dari asset InputSystem_Actions ke sini (fallback otomatis di editor).")]
        [SerializeField] private InputActionReference skill1Action;

        [Tooltip("Drag action Player/Skill2 dari asset InputSystem_Actions ke sini (fallback otomatis di editor).")]
        [SerializeField] private InputActionReference skill2Action;

        [Tooltip("Drag action Player/Skill3 dari asset InputSystem_Actions ke sini (fallback otomatis di editor).")]
        [SerializeField] private InputActionReference skill3Action;

        [Tooltip("Data hero sumber cooldown & nilai heal (opsional; bila kosong pakai default 8/12/20).")]
        [SerializeField] private CharacterData character;

        /// <summary>Data hero sumber cooldown &amp; nilai heal. Bisa di-set runtime sebelum cast.</summary>
        public CharacterData Character
        {
            get { return character; }
            set { character = value; }
        }

        /// <summary>Broadcast (indexSkill 0..2, durasiCooldown) setiap cast berhasil. UI subscribe untuk radial cooldown.</summary>
        public event Action<int, float> OnCooldownChanged;

        private const float Skill1DamageMultiplier = 1.5f;
        private const float Skill1Duration = 3f;
        private const float Skill1DefaultCooldown = 8f;

        private const int Skill2ExtraShots = 1;
        private const float Skill2Duration = 5f;
        private const float Skill2DefaultCooldown = 12f;

        private const float Skill3DefaultCooldown = 20f;
        private const float Skill3DefaultHeal = 300f;

        private PlayerStats _stats;
        private HealthSystem _health;

        private readonly float[] _nextReady = new float[3];
        private Coroutine _skill1Routine;
        private Coroutine _skill2Routine;
        private bool _skill1Active;
        private bool _skill2Active;

        private InputAction _skill1;
        private InputAction _skill2;
        private InputAction _skill3;
        private bool _ownSkill1;
        private bool _ownSkill2;
        private bool _ownSkill3;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _health = GetComponent<HealthSystem>();

            _skill1 = ResolveAction(skill1Action, "Player/Skill1", "Skill1", "<Keyboard>/1", out _ownSkill1);
            _skill2 = ResolveAction(skill2Action, "Player/Skill2", "Skill2", "<Keyboard>/2", out _ownSkill2);
            _skill3 = ResolveAction(skill3Action, "Player/Skill3", "Skill3", "<Keyboard>/3", out _ownSkill3);
        }

        /// <summary>
        /// Prioritas resolusi aksi: inspector -&gt; asset bawaan (editor) -&gt; InputAction
        /// runtime (agar skill tetap jalan di build tanpa wiring manual).
        /// </summary>
        private static InputAction ResolveAction(
            InputActionReference actionRef, string assetPath, string actionName, string binding,
            out bool owned)
        {
            owned = false;
            if (actionRef != null)
            {
                return actionRef.action;
            }

#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Settings/InputSystem_Actions.inputactions");
            var found = asset != null ? asset.FindAction(assetPath, false) : null;
            if (found != null)
            {
                return found;
            }
#endif
            var created = new InputAction(actionName, InputActionType.Button, binding);
            created.Enable();
            owned = true;
            return created;
        }

        private void OnEnable()
        {
            Subscribe(_skill1, HandleSkill1Performed);
            Subscribe(_skill2, HandleSkill2Performed);
            Subscribe(_skill3, HandleSkill3Performed);
        }

        private void OnDisable()
        {
            Unsubscribe(_skill1, HandleSkill1Performed, _ownSkill1);
            Unsubscribe(_skill2, HandleSkill2Performed, _ownSkill2);
            Unsubscribe(_skill3, HandleSkill3Performed, _ownSkill3);

            CancelBuffs();
        }

        private static void Subscribe(InputAction action, Action<InputAction.CallbackContext> handler)
        {
            if (action == null) return;
            action.Enable();
            action.performed += handler;
        }

        private static void Unsubscribe(InputAction action, Action<InputAction.CallbackContext> handler, bool owned)
        {
            if (action == null) return;
            action.performed -= handler;
            // Jangan Disable action milik asset bersama; hanya dispose yang kita buat sendiri.
            if (owned)
            {
                action.Disable();
            }
        }

        private void HandleSkill1Performed(InputAction.CallbackContext ctx) { TryCast(0); }
        private void HandleSkill2Performed(InputAction.CallbackContext ctx) { TryCast(1); }
        private void HandleSkill3Performed(InputAction.CallbackContext ctx) { TryCast(2); }

        /// <summary>
        /// Coba cast skill ke-index (0..2). Hanya jalan saat state Playing dan cooldown siap.
        /// Di luar itu (atau index invalid) diabaikan diam-diam.
        /// </summary>
        public void TryCast(int index)
        {
            if (index < 0 || index > 2) return;
            if (_stats == null || _health == null) return;

            var gm = GameManager.Instance;
            if (gm != null && gm.State != GameManager.GameState.Playing) return;
            if (Time.time < _nextReady[index]) return;

            float cooldown = GetCooldownDuration(index);
            switch (index)
            {
                case 0:
                    StartSkill1();
                    break;
                case 1:
                    StartSkill2();
                    break;
                case 2:
                    CastHeal();
                    break;
            }

            _nextReady[index] = Time.time + cooldown;
            OnCooldownChanged?.Invoke(index, cooldown);
        }

        /// <summary>Durasi cooldown skill ke-index (dari CharacterData bila valid, else default).</summary>
        public float GetCooldownDuration(int index)
        {
            switch (index)
            {
                case 0: return ResolveCooldown(0, Skill1DefaultCooldown);
                case 1: return ResolveCooldown(1, Skill2DefaultCooldown);
                case 2: return ResolveCooldown(2, Skill3DefaultCooldown);
                default: return 0f;
            }
        }

        /// <summary>Sisa cooldown (detik) skill ke-index; 0 = siap.</summary>
        public float GetRemainingCooldown(int index)
        {
            if (index < 0 || index > 2) return 0f;
            return Mathf.Max(0f, _nextReady[index] - Time.time);
        }

        private float ResolveCooldown(int index, float fallback)
        {
            if (character == null) return fallback;
            float v = index == 0 ? character.Skill1.Cooldown
                : index == 1 ? character.Skill2.Cooldown
                : character.Skill3.Cooldown;
            return v > 0f ? v : fallback;
        }

        private void StartSkill1()
        {
            // Refresh aman: kembalikan buff lama dulu agar multiplier tidak menumpuk.
            if (_skill1Active) RevertSkill1();
            if (_skill1Routine != null) StopCoroutine(_skill1Routine);

            _stats.Damage = _stats.Damage * Skill1DamageMultiplier;
            _skill1Active = true;
            _skill1Routine = StartCoroutine(RevertAfterSeconds(0, Skill1Duration));
        }

        private void StartSkill2()
        {
            if (_skill2Active) RevertSkill2();
            if (_skill2Routine != null) StopCoroutine(_skill2Routine);

            _stats.MultiShot = _stats.MultiShot + Skill2ExtraShots;
            _skill2Active = true;
            _skill2Routine = StartCoroutine(RevertAfterSeconds(1, Skill2Duration));
        }

        private void CastHeal()
        {
            float amount = Skill3DefaultHeal;
            if (character != null && character.Skill3.Value > 0f) amount = character.Skill3.Value;
            _health.Heal(amount);
        }

        private IEnumerator RevertAfterSeconds(int index, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (index == 0) RevertSkill1();
            else if (index == 1) RevertSkill2();
        }

        private void RevertSkill1()
        {
            if (!_skill1Active) return;
            _skill1Active = false;
            // Bagi balik (bukan restore snapshot) agar upgrade yang masuk di tengah buff tidak hilang.
            _stats.Damage = _stats.Damage / Skill1DamageMultiplier;
        }

        private void RevertSkill2()
        {
            if (!_skill2Active) return;
            _skill2Active = false;
            _stats.MultiShot = Mathf.Max(1, _stats.MultiShot - Skill2ExtraShots);
        }

        private void CancelBuffs()
        {
            if (_skill1Routine != null) StopCoroutine(_skill1Routine);
            if (_skill2Routine != null) StopCoroutine(_skill2Routine);
            _skill1Routine = null;
            _skill2Routine = null;

            // Hanya revert bila komponen masih punya referensi valid (hindari NRE saat teardown).
            if (_stats != null)
            {
                RevertSkill1();
                RevertSkill2();
            }
            else
            {
                _skill1Active = false;
                _skill2Active = false;
            }
        }
    }
}
