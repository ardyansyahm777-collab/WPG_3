using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WpgGame.Combat;
using WpgGame.Progression;
using WpgGame.Economy;
using WpgGame.Core;
using WpgGame.Player;

namespace WpgGame.UI
{
    /// <summary>
    /// HUD: HP bar, EXP bar + level, gold (kontrak API bagian 12) + aditif
    /// Workstream A: shield bar + angka, timer run M:SS, kill counter,
    /// portrait inisial hero. Hanya subscribe event + baca state.
    /// Kontrak API bagian 12: Bind(HealthSystem, LevelUpSystem, GoldManager) TIDAK berubah.
    /// Overload + BindCharacter untuk wiring eksplisit (bootstrap/inspector).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Bars")]
        [SerializeField] private Slider hpBar;
        [SerializeField] private Slider expBar;
        [SerializeField] private Slider shieldBar;

        [Header("Texts")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text shieldText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text killText;

        [Header("Portrait (Image hiasan + TMP inisial fallback PortraitText)")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text portraitText;

        private HealthSystem boundHealth;
        private LevelUpSystem boundLevels;
        private GoldManager boundGold;
        private RunStats boundRunStats;
        private CharacterData boundCharacter;
        private bool isBound;
        private int killCount;

        private void OnEnable()
        {
            // Reset per run (reload scene = instance baru; aman bila reuse via SetActive).
            killCount = 0;
            UpdateKillText();

            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnVictory += HandleVictory;

            if (!isBound)
            {
                // HealthSystem dipakai player DAN enemy — wajib ambil dari player (tag Player),
                // fallback ke instance pertama bila player belum ada.
                var player = GameObject.FindGameObjectWithTag("Player");
                var health = player != null
                    ? player.GetComponent<HealthSystem>()
                    : FindFirstObjectByType<HealthSystem>();
                var levels = FindFirstObjectByType<LevelUpSystem>();
                var gold = FindFirstObjectByType<GoldManager>();

                if (health != null && levels != null && gold != null)
                {
                    Bind(health, levels, gold);
                }
                else
                {
                    EnsureRunStatsSubscription();
                    ResolvePortrait();
                    RefreshShieldImmediate();
                    RefreshTimerImmediate();
                }
            }
            else
            {
                EnsureRunStatsSubscription();
                ResolvePortrait();
            }
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnVictory -= HandleVictory;
            Unbind();
        }

        /// <summary>
        /// Bind ke tiga sistem (KONTRAK API bagian 12 — signature tidak berubah).
        /// Guard double-bind: Unbind dulu bila sudah bound. Auto-resolve RunStats +
        /// portrait + refresh-once semua tampilan.
        /// </summary>
        public void Bind(HealthSystem playerHealth, LevelUpSystem levelSystem, GoldManager gold)
        {
            Bind(playerHealth, levelSystem, gold, null);
        }

        /// <summary>
        /// Overload aditif: RunStats eksplisit (null = auto-find). Signature 3-arg di atas tetap.
        /// </summary>
        public void Bind(HealthSystem playerHealth, LevelUpSystem levelSystem, GoldManager gold, RunStats runStats)
        {
            if (isBound)
            {
                Unbind();
            }

            boundHealth = playerHealth;
            boundLevels = levelSystem;
            boundGold = gold;
            boundRunStats = runStats != null ? runStats : FindFirstObjectByType<RunStats>();

            if (boundHealth != null)
            {
                boundHealth.OnHealthChanged += HandleHealthChanged;
                boundHealth.OnShieldChanged += HandleShieldChanged;
            }

            if (boundLevels != null)
            {
                boundLevels.OnExpChanged += HandleExpChanged;
                boundLevels.OnLevelChanged += HandleLevelChanged;
            }

            if (boundGold != null)
            {
                boundGold.OnGoldChanged += HandleGoldChanged;
            }

            if (boundRunStats != null)
            {
                boundRunStats.OnTimerChanged += HandleTimerChanged;
            }

            isBound = true;

            // Refresh langsung sekali saat Bind (baca state, bukan panggil method internal).
            if (boundHealth != null)
            {
                HandleHealthChanged(boundHealth.CurrentHP, boundHealth.MaxHP);
                HandleShieldChanged(boundHealth.ShieldPoints, boundHealth.ShieldMax);
            }
            else
            {
                RefreshShieldImmediate();
            }

            if (boundLevels != null)
            {
                HandleExpChanged(boundLevels.CurrentExp, boundLevels.ExpToNextLevel);
                HandleLevelChanged(boundLevels.CurrentLevel);
            }

            if (boundGold != null)
            {
                HandleGoldChanged(boundGold.CurrentGold);
            }

            if (boundRunStats != null)
            {
                HandleTimerChanged(boundRunStats.RemainingSeconds);
            }
            else
            {
                RefreshTimerImmediate();
            }

            UpdateKillText();
            ResolvePortrait();
        }

        /// <summary>
        /// Wiring eksplisit CharacterData untuk portrait (bootstrap/inspector).
        /// Null = resolve otomatis (skill aktif / PendingCharacter).
        /// </summary>
        public void BindCharacter(CharacterData character)
        {
            boundCharacter = character;
            ApplyPortrait(boundCharacter != null ? boundCharacter : TryResolveCharacterData());
        }

        private void Unbind()
        {
            if (boundHealth != null)
            {
                boundHealth.OnHealthChanged -= HandleHealthChanged;
                boundHealth.OnShieldChanged -= HandleShieldChanged;
            }

            if (boundLevels != null)
            {
                boundLevels.OnExpChanged -= HandleExpChanged;
                boundLevels.OnLevelChanged -= HandleLevelChanged;
            }

            if (boundGold != null)
            {
                boundGold.OnGoldChanged -= HandleGoldChanged;
            }

            if (boundRunStats != null)
            {
                boundRunStats.OnTimerChanged -= HandleTimerChanged;
            }

            boundHealth = null;
            boundLevels = null;
            boundGold = null;
            boundRunStats = null;
            isBound = false;
        }

        private void EnsureRunStatsSubscription()
        {
            if (boundRunStats != null)
            {
                return;
            }

            var run = FindFirstObjectByType<RunStats>();
            if (run == null)
            {
                return;
            }

            boundRunStats = run;
            boundRunStats.OnTimerChanged += HandleTimerChanged;
            HandleTimerChanged(boundRunStats.RemainingSeconds);
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (hpBar == null)
            {
                return;
            }

            hpBar.minValue = 0f;
            hpBar.maxValue = 1f;
            hpBar.value = max <= 0f ? 0f : Mathf.Clamp01(current / max);
        }

        private void HandleShieldChanged(float current, float max)
        {
            if (shieldBar != null)
            {
                shieldBar.minValue = 0f;
                shieldBar.maxValue = 1f;
                shieldBar.value = max <= 0f ? 0f : Mathf.Clamp01(current / max);
            }

            if (shieldText != null)
            {
                shieldText.text = (max <= 0f || current <= 0f)
                    ? "0"
                    : Mathf.Max(0, Mathf.RoundToInt(current)).ToString();
            }
        }

        private void RefreshShieldImmediate()
        {
            if (boundHealth != null)
            {
                HandleShieldChanged(boundHealth.ShieldPoints, boundHealth.ShieldMax);
                return;
            }

            if (shieldBar != null)
            {
                shieldBar.minValue = 0f;
                shieldBar.maxValue = 1f;
                shieldBar.value = 0f;
            }

            if (shieldText != null)
            {
                shieldText.text = "0";
            }
        }

        private void HandleExpChanged(float current, float toNext)
        {
            if (expBar != null)
            {
                expBar.minValue = 0f;
                expBar.maxValue = 1f;
                expBar.value = toNext <= 0f ? 0f : Mathf.Clamp01(current / toNext);
            }

            if (levelText != null && boundLevels != null)
            {
                levelText.text = "Lv " + boundLevels.CurrentLevel;
            }
        }

        private void HandleLevelChanged(int newLevel)
        {
            if (levelText != null)
            {
                levelText.text = "Lv " + newLevel;
            }
        }

        private void HandleGoldChanged(int total)
        {
            if (goldText != null)
            {
                goldText.text = total.ToString();
            }
        }

        private void HandleTimerChanged(float remaining)
        {
            if (timerText != null)
            {
                timerText.text = FormatTime(remaining);
            }
        }

        private void RefreshTimerImmediate()
        {
            if (boundRunStats != null)
            {
                HandleTimerChanged(boundRunStats.RemainingSeconds);
                return;
            }

            if (timerText != null)
            {
                timerText.text = FormatTime(RunStats.DefaultDuration);
            }
        }

        private void HandleEnemyKilled(GameObject enemy)
        {
            killCount++;
            UpdateKillText();
        }

        private void UpdateKillText()
        {
            if (killText != null)
            {
                killText.text = "KILL " + killCount;
            }
        }

        private void HandleVictory()
        {
            // Timer sudah 0 via RunStats.OnTimerChanged sebelum RaiseVictory;
            // pastikan final. Panel menang milik GameOverScreen (mode VICTORY).
            UpdateTimerTextZero();
        }

        private void UpdateTimerTextZero()
        {
            if (timerText != null)
            {
                timerText.text = FormatTime(0f);
            }
        }

        private void ResolvePortrait()
        {
            CharacterData data = boundCharacter != null ? boundCharacter : TryResolveCharacterData();
            ApplyPortrait(data);
        }

        /// <summary>
        /// Baca state saja (tanpa panggil method internal): skill aktif -&gt; PendingCharacter.
        /// </summary>
        private CharacterData TryResolveCharacterData()
        {
            if (boundHealth != null)
            {
                var aelindra = boundHealth.GetComponent<AelindraSkill>();
                if (aelindra != null && aelindra.Character != null)
                {
                    return aelindra.Character;
                }

                var hero = boundHealth.GetComponent<HeroSkill>();
                if (hero != null && hero.Character != null)
                {
                    return hero.Character;
                }
            }

            var pending = GameplaySetup.PendingCharacter;
            if (pending != null)
            {
                return pending;
            }

            return null;
        }

        private void ApplyPortrait(CharacterData data)
        {
            string initials = "AE";
            if (data != null && !string.IsNullOrEmpty(data.PortraitText))
            {
                initials = data.PortraitText;
            }
            else if (data != null && !string.IsNullOrEmpty(data.DisplayName))
            {
                initials = data.DisplayName.Length >= 2
                    ? data.DisplayName.Substring(0, 2).ToUpper()
                    : data.DisplayName.Substring(0, 1).ToUpper();
            }

            if (portraitText != null)
            {
                portraitText.text = initials;
            }
            // portraitImage sengaja dibiarkan sebagai background placeholder
            // (CharacterData tidak membawa Sprite; bootstrap memberi warna).
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int m = total / 60;
            int s = total % 60;
            return m + ":" + s.ToString("00");
        }
    }
}
