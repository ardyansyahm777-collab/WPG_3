using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WpgGame.Combat;
using WpgGame.Progression;
using WpgGame.Economy;

namespace WpgGame.UI
{
    /// <summary>
    /// HUD: HP bar, EXP bar + level, gold. Hanya subscribe event + baca state.
    /// Kontrak API bagian 12: Bind(HealthSystem, LevelUpSystem, GoldManager).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Bars")]
        [SerializeField] private Slider hpBar;
        [SerializeField] private Slider expBar;

        [Header("Texts")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text goldText;

        private HealthSystem boundHealth;
        private LevelUpSystem boundLevels;
        private GoldManager boundGold;
        private bool isBound;

        private void OnEnable()
        {
            if (isBound)
            {
                return;
            }

            var health = FindFirstObjectByType<HealthSystem>();
            var levels = FindFirstObjectByType<LevelUpSystem>();
            var gold = FindFirstObjectByType<GoldManager>();

            if (health != null && levels != null && gold != null)
            {
                Bind(health, levels, gold);
            }
        }

        private void OnDisable()
        {
            Unbind();
        }

        /// <summary>
        /// Bind ke tiga sistem. Guard double-bind: Unbind dulu bila sudah bound.
        /// </summary>
        public void Bind(HealthSystem playerHealth, LevelUpSystem levelSystem, GoldManager gold)
        {
            if (isBound)
            {
                Unbind();
            }

            boundHealth = playerHealth;
            boundLevels = levelSystem;
            boundGold = gold;

            if (boundHealth != null)
            {
                boundHealth.OnHealthChanged += HandleHealthChanged;
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

            isBound = true;

            // Refresh langsung sekali saat Bind (baca state, bukan panggil method internal).
            if (boundHealth != null)
            {
                HandleHealthChanged(boundHealth.CurrentHP, boundHealth.MaxHP);
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
        }

        private void Unbind()
        {
            if (!isBound)
            {
                return;
            }

            if (boundHealth != null)
            {
                boundHealth.OnHealthChanged -= HandleHealthChanged;
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

            boundHealth = null;
            boundLevels = null;
            boundGold = null;
            isBound = false;
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
    }
}
