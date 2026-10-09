using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using WpgGame.Combat;
using WpgGame.Core;
using WpgGame.Enemy;

namespace WpgGame.Player
{
    /// <summary>
    /// Kit khusus Aelindra. API cermin <see cref="HeroSkill"/> agar UI bisa bind seragam:
    /// index 0 = shield (Skill1), index 1 = barier (Skill2).
    /// - Skill1: shield 30% MaxHP, durasi 5 dtk, CD 12 (via HealthSystem.GrantShield).
    /// - Skill2: barier lingkaran R3 statis di titik cast, durasi 6 dtk, CD 18
    ///   (via AelindraBarrier; menahan musuh biasa, boss lewat).
    /// - Pasif chain lightning: tiap basic attack yang MENGENAI musuh roll 16% +
    ///   pity (reset tiap proc, guarantee hit ke-10); chain = 50% damage awal ke
    ///   3 musuh terdekat dalam radius 6 + garis petir pooled (0.15 dtk).
    /// Cooldown &amp; nilai baca dari <see cref="CharacterData"/> (Skill1/Skill2),
    /// fallback ke konstanta bila kosong. Input dari action Player/Skill1 &amp;
    /// Player/Skill2 (pola ResolveAction seperti HeroSkill + fallback runtime).
    /// Freeze: tolak cast &amp; proc saat GameManager.State bukan Playing.
    /// Nonaktif otomatis bila Character terisi hero lain (HeroSkill generik yang jalan).
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(AutoAimShooter))]
    public class AelindraSkill : MonoBehaviour
    {
        [Tooltip("Drag action Player/Skill1 dari asset InputSystem_Actions ke sini (fallback otomatis).")]
        [SerializeField] private InputActionReference skill1Action;

        [Tooltip("Drag action Player/Skill2 dari asset InputSystem_Actions ke sini (fallback otomatis).")]
        [SerializeField] private InputActionReference skill2Action;

        [Tooltip("Data hero Aelindra sumber cooldown & nilai (di-set GameplaySetup).")]
        [SerializeField] private CharacterData character;

        /// <summary>Data hero sumber cooldown &amp; nilai. Bisa di-set runtime sebelum cast.</summary>
        public CharacterData Character
        {
            get { return character; }
            set { character = value; }
        }

        /// <summary>Broadcast (indexSkill 0..1, durasiCooldown) setiap cast berhasil. UI subscribe untuk radial cooldown.</summary>
        public event Action<int, float> OnCooldownChanged;

        // --- Fallback bila CharacterData kosong (sesuai spek rombak Aelindra) ---
        private const float S1DefaultCooldown = 12f;
        private const float S1AbsorbPct = 30f;
        private const float S1Duration = 5f;

        private const float S2DefaultCooldown = 18f;
        private const float S2FallbackRadius = 3f;
        private const float S2Duration = 6f;

        // --- Pasif chain lightning ---
        private const float ChainProcChance = 0.16f;
        private const int ChainPityHits = 10;
        private const float ChainDamagePct = 50f;
        private const float ChainRadius = 6f;
        private const int ChainMaxTargets = 3;
        private const float BoltLifetime = 0.15f;
        private const int BoltPoolSize = 6;

        private static readonly Color BoltCyan = new Color(0.35f, 1f, 1f, 1f);
        private static readonly Color BoltWhite = new Color(0.9f, 1f, 1f, 1f);
        private static readonly Color ProjectileCyan = new Color(0.25f, 0.95f, 1f, 1f);
        private static Material _boltMaterial;

        private PlayerStats _stats;
        private HealthSystem _health;
        private AutoAimShooter _shooter;

        private readonly float[] _nextReady = new float[2];
        private float _shieldTimeLeft;

        private InputAction _skill1;
        private InputAction _skill2;
        private bool _ownSkill1;
        private bool _ownSkill2;

        private int _hitsSinceProc;
        private AelindraBarrier _barrier;

        private readonly List<LineRenderer> _bolts = new List<LineRenderer>(BoltPoolSize);
        private readonly List<float> _boltHideAt = new List<float>(BoltPoolSize);

        private GameObject _cyanProjectilePrefab;
        private GameObject _originalProjectilePrefab;
        private bool _tintApplied;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _health = GetComponent<HealthSystem>();
            _shooter = GetComponent<AutoAimShooter>();

            _skill1 = ResolveAction(skill1Action, "Player/Skill1", "Skill1", "<Keyboard>/1", out _ownSkill1);
            _skill2 = ResolveAction(skill2Action, "Player/Skill2", "Skill2", "<Keyboard>/2", out _ownSkill2);
        }

        /// <summary>
        /// Prioritas resolusi aksi: inspector -&gt; asset bawaan (editor) -&gt; InputAction
        /// runtime (agar skill tetap jalan di build tanpa wiring manual). Cermin HeroSkill.
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
            Projectile.OnProjectileHit += HandleProjectileHit;
        }

        private void OnDisable()
        {
            Unsubscribe(_skill1, HandleSkill1Performed, _ownSkill1);
            Unsubscribe(_skill2, HandleSkill2Performed, _ownSkill2);
            Projectile.OnProjectileHit -= HandleProjectileHit;

            // Kembalikan template proyektil generik agar hero lain tak ikut ke-tint.
            if (_tintApplied && _shooter != null && _originalProjectilePrefab != null)
            {
                _shooter.ProjectilePrefab = _originalProjectilePrefab;
            }
            _tintApplied = false;
            _cyanProjectilePrefab = null;
            _originalProjectilePrefab = null;
        }

        private void Start()
        {
            // Pengaman bila dipasang manual ke hero lain tanpa lewat GameplaySetup.
            if (character != null && character.Id != "aelindra")
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            TryApplyCyanTint();
            TickShield();
            HideExpiredBolts();
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

        /// <summary>
        /// Coba cast skill Aelindra: 0 = shield, 1 = barier. Hanya jalan saat state
        /// Playing dan cooldown siap; di luar itu (atau index invalid) diabaikan diam-diam.
        /// </summary>
        public void TryCast(int index)
        {
            if (index < 0 || index > 1) return;
            if (_stats == null || _health == null) return;
            if (character != null && character.Id != "aelindra") return;

            var gm = GameManager.Instance;
            if (gm != null && gm.State != GameManager.GameState.Playing) return;
            if (Time.time < _nextReady[index]) return;

            float cooldown = GetCooldownDuration(index);
            if (index == 0) CastShield();
            else CastBarrier();

            _nextReady[index] = Time.time + Mathf.Max(0.1f, cooldown);
            OnCooldownChanged?.Invoke(index, cooldown);
        }

        /// <summary>Durasi cooldown skill ke-index (dari CharacterData bila valid, else default).</summary>
        public float GetCooldownDuration(int index)
        {
            switch (index)
            {
                case 0:
                    return character != null && character.Skill1.Cooldown > 0f
                        ? character.Skill1.Cooldown : S1DefaultCooldown;
                case 1:
                    return character != null && character.Skill2.Cooldown > 0f
                        ? character.Skill2.Cooldown : S2DefaultCooldown;
                default:
                    return 0f;
            }
        }

        /// <summary>Sisa cooldown (detik) skill ke-index; 0 = siap.</summary>
        public float GetRemainingCooldown(int index)
        {
            if (index < 0 || index > 1) return 0f;
            return Mathf.Max(0f, _nextReady[index] - Time.time);
        }

        // ================= Skill1: shield =================

        private void CastShield()
        {
            if (_health == null) return;
            float pct = character != null && character.Skill1.Value > 0f
                ? character.Skill1.Value : S1AbsorbPct;
            float amount = Mathf.Max(0f, _health.MaxHP) * pct / 100f;
            _health.GrantShield(amount); // refresh, bukan stack
            _shieldTimeLeft = S1Duration;
        }

        private void TickShield()
        {
            if (_shieldTimeLeft <= 0f) return;
            // Freeze: durasi hanya berkurang saat Playing.
            var gm = GameManager.Instance;
            if (gm != null && gm.State != GameManager.GameState.Playing) return;

            _shieldTimeLeft -= Time.deltaTime;
            if (_shieldTimeLeft <= 0f)
            {
                _shieldTimeLeft = 0f;
                if (_health != null) _health.GrantShield(0f); // kosongkan sisa shield
            }
        }

        // ================= Skill2: barier =================

        private void CastBarrier()
        {
            float radius = character != null && character.Skill2.Value > 0f
                ? character.Skill2.Value : S2FallbackRadius;
            if (_barrier == null)
            {
                _barrier = AelindraBarrier.Build(transform.position, radius);
            }
            var playerColliders = GetComponents<Collider2D>();
            _barrier.Activate(transform.position, radius, S2Duration, playerColliders);
        }

        // ================= Pasif: chain lightning =================

        private void HandleProjectileHit(GameObject victim, float damage)
        {
            if (victim == null || damage <= 0f) return;
            if (character != null && character.Id != "aelindra") return;

            // Freeze: tidak proc saat bukan Playing. (Projectile juga tidak memberi
            // damage saat freeze, jadi event ini praktis hanya datang saat Playing.)
            var gm = GameManager.Instance;
            if (gm != null && gm.State != GameManager.GameState.Playing) return;

            _hitsSinceProc++;
            bool proc = _hitsSinceProc >= ChainPityHits || UnityEngine.Random.value < ChainProcChance;
            if (!proc) return;

            _hitsSinceProc = 0;
            FireChain(victim, damage);
        }

        private void FireChain(GameObject firstVictim, float baseDamage)
        {
            Vector2 origin = firstVictim.transform.position;
            float sqrRange = ChainRadius * ChainRadius;
            float chainDamage = baseDamage * ChainDamagePct / 100f;

            // Pilih 3 musuh terdekat dalam radius (di luar target pertama).
            var best = new EnemyAI[ChainMaxTargets];
            var bestSqr = new float[ChainMaxTargets];
            for (int i = 0; i < ChainMaxTargets; i++) bestSqr[i] = sqrRange;

            var all = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var e = all[i];
                if (e == null || e.gameObject == firstVictim) continue;
                if (!e.gameObject.activeInHierarchy) continue;
                var hs = e.Health != null ? e.Health : e.GetComponent<HealthSystem>();
                if (hs == null || hs.IsDead) continue;

                float sqr = ((Vector2)e.transform.position - origin).sqrMagnitude;
                for (int k = 0; k < ChainMaxTargets; k++)
                {
                    if (sqr < bestSqr[k])
                    {
                        for (int m = ChainMaxTargets - 1; m > k; m--)
                        {
                            best[m] = best[m - 1];
                            bestSqr[m] = bestSqr[m - 1];
                        }
                        best[k] = e;
                        bestSqr[k] = sqr;
                        break;
                    }
                }
            }

            EnsureBolts();
            for (int i = 0; i < ChainMaxTargets; i++)
            {
                var target = best[i];
                if (target == null) continue;
                var hs = target.Health != null ? target.Health : target.GetComponent<HealthSystem>();
                if (hs == null || hs.IsDead) continue;

                Vector2 to = target.transform.position;
                hs.TakeDamage(chainDamage); // kill → broadcast EnemyKilled seperti biasa
                SpawnBolt(origin, to);
            }
        }

        // ================= Visual petir (pooled LineRenderer) =================

        private void EnsureBolts()
        {
            if (_bolts.Count > 0) return;
            for (int i = 0; i < BoltPoolSize; i++)
            {
                var go = new GameObject("AelindraBolt");
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 3;
                lr.startWidth = 0.09f;
                lr.endWidth = 0.09f;
                lr.startColor = BoltWhite;
                lr.endColor = BoltCyan;
                lr.sortingOrder = 5;
                if (_boltMaterial == null)
                {
                    var shader = Shader.Find("Sprites/Default");
                    if (shader != null)
                    {
                        _boltMaterial = new Material(shader);
                        _boltMaterial.name = "aelindra_bolt_mat";
                    }
                }
                if (_boltMaterial != null) lr.material = _boltMaterial;
                go.SetActive(false);
                RuntimeSpawnTag.Tag(go, "WpgGame.Player.AelindraSkill.EnsureBolts");
                _bolts.Add(lr);
                _boltHideAt.Add(0f);
            }
        }

        private void SpawnBolt(Vector2 from, Vector2 to)
        {
            LineRenderer free = null;
            int freeIndex = -1;
            for (int i = 0; i < _bolts.Count; i++)
            {
                if (!_bolts[i].gameObject.activeSelf)
                {
                    free = _bolts[i];
                    freeIndex = i;
                    break;
                }
            }
            if (free == null) return; // pool habis (max 3/proc, pool 6): damage tetap jalan.

            // Zigzag sederhana: titik tengah digeser acak tegak lurus arah.
            Vector2 dir = to - from;
            Vector2 mid = (from + to) * 0.5f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
                mid += perp * UnityEngine.Random.Range(-0.35f, 0.35f);
            }
            free.SetPosition(0, new Vector3(from.x, from.y, -1f));
            free.SetPosition(1, new Vector3(mid.x, mid.y, -1f));
            free.SetPosition(2, new Vector3(to.x, to.y, -1f));
            free.gameObject.SetActive(true);
            _boltHideAt[freeIndex] = Time.time + BoltLifetime;
        }

        private void HideExpiredBolts()
        {
            for (int i = 0; i < _bolts.Count; i++)
            {
                if (_bolts[i].gameObject.activeSelf && Time.time >= _boltHideAt[i])
                {
                    _bolts[i].gameObject.SetActive(false);
                }
            }
        }

        // ================= Recolor proyektil (tint cyan elektrik) =================

        private void TryApplyCyanTint()
        {
            if (_tintApplied || _shooter == null) return;
            var prefab = _shooter.ProjectilePrefab;
            if (prefab == null) return;

            // Template sudah versi cyan milik kita (mis. habis disable/enable ulang).
            if (prefab == _cyanProjectilePrefab)
            {
                _tintApplied = true;
                return;
            }

            // Clone template generik → tint cyan → pasang ke shooter. Template asli
            // tak disentuh sehingga hero lain tetap pakai warna generik.
            _originalProjectilePrefab = prefab;
            var clone = Instantiate(prefab);
            clone.name = prefab.name + "_AelindraCyan";
            clone.SetActive(false); // template harus tetap inactive
            var sr = clone.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = ProjectileCyan;
            if (Application.isPlaying) PrefabPool.GetOrCreate(clone, 16, 120);
            _shooter.ProjectilePrefab = clone;
            _cyanProjectilePrefab = clone;
            _tintApplied = true;
        }
    }
}
