using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WpgGame.Player;

namespace WpgGame.UI
{
    /// <summary>
    /// Skill bar bawah-tengah: 2 slot Button aktif + 1 slot pasif display.
    /// Bind ke kedua sistem skill: resolve saat bind — cari AelindraSkill dulu,
    /// fallback HeroSkill. Hanya API publik: TryCast / GetRemainingCooldown /
    /// GetCooldownDuration / OnCooldownChanged (+ Character untuk label/pasif).
    /// Klik/tap Button cast (mouse+touch); keyboard 1/2 tetap via sistem skill.
    /// Overlay cooldown radial (fillAmount) + tombol non-interactable + redup
    /// via CanvasGroup. Poll di Update HANYA saat ada cooldown berjalan.
    /// </summary>
    public class SkillBarUI : MonoBehaviour
    {
        [Header("Skill Buttons")]
        [SerializeField] private Button skill1Button;
        [SerializeField] private Button skill2Button;
        [SerializeField] private Image skill1CooldownOverlay;
        [SerializeField] private Image skill2CooldownOverlay;
        [SerializeField] private TMP_Text skill1CooldownText;
        [SerializeField] private TMP_Text skill2CooldownText;
        [SerializeField] private TMP_Text skill1NameText;
        [SerializeField] private TMP_Text skill2NameText;

        [Header("Passive (display statis: ikon hiasan + nama + desc)")]
        [SerializeField] private Image passiveIcon;
        [SerializeField] private TMP_Text passiveNameText;
        [SerializeField] private TMP_Text passiveDescText;

        private AelindraSkill aelindraSkill;
        private HeroSkill heroSkill;
        private bool useAelindra;
        private CharacterData boundCharacter;
        private bool isBound;
        private bool pollCooldowns;
        private int lastS1Sec = -1;
        private int lastS2Sec = -1;
        private CanvasGroup s1Group;
        private CanvasGroup s2Group;

        private void OnEnable()
        {
            if (isBound)
            {
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Bind(player);
            }
        }

        private void Start()
        {
            // Player dibuat via GameplaySetup.sceneLoaded — bisa belum ada saat OnEnable.
            if (isBound)
            {
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Bind(player);
            }
        }

        private void OnDisable()
        {
            Unbind();
        }

        /// <summary>
        /// Bind ke player: resolve AelindraSkill dulu, fallback HeroSkill.
        /// Setup label skill/pasif dari CharacterData, subscribe cooldown, refresh-once.
        /// </summary>
        public void Bind(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            if (isBound)
            {
                Unbind();
            }

            aelindraSkill = player.GetComponent<AelindraSkill>();
            heroSkill = player.GetComponent<HeroSkill>();

            if (aelindraSkill != null && aelindraSkill.enabled)
            {
                useAelindra = true;
            }
            else if (heroSkill != null && heroSkill.enabled)
            {
                useAelindra = false;
            }
            else if (aelindraSkill != null)
            {
                useAelindra = true;
            }
            else if (heroSkill != null)
            {
                useAelindra = false;
            }
            else
            {
                return;
            }

            boundCharacter = ResolveCharacter(player);

            if (useAelindra)
            {
                aelindraSkill.OnCooldownChanged += HandleCooldownChanged;
            }
            else
            {
                heroSkill.OnCooldownChanged += HandleCooldownChanged;
            }

            if (skill1Button != null)
            {
                skill1Button.onClick.AddListener(OnSkill1Clicked);
            }

            if (skill2Button != null)
            {
                skill2Button.onClick.AddListener(OnSkill2Clicked);
            }

            s1Group = skill1Button != null ? skill1Button.GetComponent<CanvasGroup>() : null;
            s2Group = skill2Button != null ? skill2Button.GetComponent<CanvasGroup>() : null;

            isBound = true;
            lastS1Sec = -1;
            lastS2Sec = -1;

            SetupLabels();
            RefreshSlotsImmediate();
            pollCooldowns = HasAnyCooldown();
        }

        /// <summary>Overload: CharacterData eksplisit (bootstrap). Null = resolve otomatis.</summary>
        public void Bind(GameObject player, CharacterData character)
        {
            Bind(player);
            if (character != null)
            {
                BindCharacter(character);
            }
        }

        /// <summary>Wiring eksplisit CharacterData untuk label skill + slot pasif.</summary>
        public void BindCharacter(CharacterData character)
        {
            boundCharacter = character;
            SetupLabels();
        }

        private void Unbind()
        {
            if (aelindraSkill != null)
            {
                aelindraSkill.OnCooldownChanged -= HandleCooldownChanged;
            }

            if (heroSkill != null)
            {
                heroSkill.OnCooldownChanged -= HandleCooldownChanged;
            }

            if (skill1Button != null)
            {
                skill1Button.onClick.RemoveListener(OnSkill1Clicked);
            }

            if (skill2Button != null)
            {
                skill2Button.onClick.RemoveListener(OnSkill2Clicked);
            }

            aelindraSkill = null;
            heroSkill = null;
            s1Group = null;
            s2Group = null;
            isBound = false;
            pollCooldowns = false;
            lastS1Sec = -1;
            lastS2Sec = -1;
        }

        private void OnSkill1Clicked()
        {
            if (!isBound)
            {
                return;
            }

            if (useAelindra)
            {
                if (aelindraSkill != null)
                {
                    aelindraSkill.TryCast(0);
                }
            }
            else
            {
                if (heroSkill != null)
                {
                    heroSkill.TryCast(0);
                }
            }
        }

        private void OnSkill2Clicked()
        {
            if (!isBound)
            {
                return;
            }

            if (useAelindra)
            {
                if (aelindraSkill != null)
                {
                    aelindraSkill.TryCast(1);
                }
            }
            else
            {
                if (heroSkill != null)
                {
                    heroSkill.TryCast(1);
                }
            }
        }

        private void HandleCooldownChanged(int index, float duration)
        {
            lastS1Sec = -1;
            lastS2Sec = -1;
            pollCooldowns = true;
        }

        private void Update()
        {
            if (!isBound || !pollCooldowns)
            {
                return;
            }

            UpdateSlot(0);
            UpdateSlot(1);

            if (!HasAnyCooldown())
            {
                pollCooldowns = false;
            }
        }

        private void UpdateSlot(int slot)
        {
            float rem = GetRemaining(slot);
            float dur = GetDuration(slot);
            bool ready = rem <= 0f;

            Button btn = slot == 0 ? skill1Button : skill2Button;
            Image overlay = slot == 0 ? skill1CooldownOverlay : skill2CooldownOverlay;
            TMP_Text cdText = slot == 0 ? skill1CooldownText : skill2CooldownText;
            CanvasGroup group = slot == 0 ? s1Group : s2Group;
            int lastSec = slot == 0 ? lastS1Sec : lastS2Sec;

            if (overlay != null)
            {
                bool showOverlay = !ready && dur > 0f;
                if (overlay.gameObject.activeSelf != showOverlay)
                {
                    overlay.gameObject.SetActive(showOverlay);
                }

                if (showOverlay)
                {
                    overlay.fillAmount = Mathf.Clamp01(rem / dur);
                }
            }

            if (btn != null && btn.interactable == ready)
            {
                // Hanya sentuh saat berubah (hindari dirty layout per-frame).
            }
            else if (btn != null)
            {
                btn.interactable = ready;
            }

            if (group != null)
            {
                float want = ready ? 1f : 0.5f;
                if (!Mathf.Approximately(group.alpha, want))
                {
                    group.alpha = want;
                }
            }

            if (cdText != null)
            {
                if (ready)
                {
                    if (cdText.gameObject.activeSelf)
                    {
                        cdText.gameObject.SetActive(false);
                    }

                    if (slot == 0)
                    {
                        lastS1Sec = -1;
                    }
                    else
                    {
                        lastS2Sec = -1;
                    }
                }
                else
                {
                    int sec = Mathf.Max(1, Mathf.CeilToInt(rem));
                    if (!cdText.gameObject.activeSelf)
                    {
                        cdText.gameObject.SetActive(true);
                    }

                    if (sec != lastSec)
                    {
                        cdText.text = sec.ToString();
                        if (slot == 0)
                        {
                            lastS1Sec = sec;
                        }
                        else
                        {
                            lastS2Sec = sec;
                        }
                    }
                }
            }
        }

        private float GetRemaining(int slot)
        {
            if (useAelindra)
            {
                return aelindraSkill != null ? aelindraSkill.GetRemainingCooldown(slot) : 0f;
            }

            return heroSkill != null ? heroSkill.GetRemainingCooldown(slot) : 0f;
        }

        private float GetDuration(int slot)
        {
            if (useAelindra)
            {
                return aelindraSkill != null ? aelindraSkill.GetCooldownDuration(slot) : 0f;
            }

            return heroSkill != null ? heroSkill.GetCooldownDuration(slot) : 0f;
        }

        private bool HasAnyCooldown()
        {
            return GetRemaining(0) > 0f || GetRemaining(1) > 0f;
        }

        private void RefreshSlotsImmediate()
        {
            // Paksa overlay sinkron sekali (tanpa tunggu event): set lastSec -1 lalu update.
            lastS1Sec = -1;
            lastS2Sec = -1;
            UpdateSlot(0);
            UpdateSlot(1);
        }

        private CharacterData ResolveCharacter(GameObject player)
        {
            if (boundCharacter != null)
            {
                return boundCharacter;
            }

            if (useAelindra && aelindraSkill != null && aelindraSkill.Character != null)
            {
                return aelindraSkill.Character;
            }

            if (!useAelindra && heroSkill != null && heroSkill.Character != null)
            {
                return heroSkill.Character;
            }

            if (aelindraSkill != null && aelindraSkill.Character != null)
            {
                return aelindraSkill.Character;
            }

            if (heroSkill != null && heroSkill.Character != null)
            {
                return heroSkill.Character;
            }

            var pending = GameplaySetup.PendingCharacter;
            if (pending != null)
            {
                return pending;
            }

            return null;
        }

        private void SetupLabels()
        {
            CharacterData c = boundCharacter;
            if (c == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                c = player != null ? ResolveCharacter(player) : null;
                if (c == null)
                {
                    if (skill1NameText != null && string.IsNullOrEmpty(skill1NameText.text))
                    {
                        skill1NameText.text = "Skill 1";
                    }

                    if (skill2NameText != null && string.IsNullOrEmpty(skill2NameText.text))
                    {
                        skill2NameText.text = "Skill 2";
                    }

                    if (passiveNameText != null && string.IsNullOrEmpty(passiveNameText.text))
                    {
                        passiveNameText.text = "Passive";
                    }

                    return;
                }

                boundCharacter = c;
            }

            if (skill1NameText != null)
            {
                skill1NameText.text = string.IsNullOrEmpty(c.Skill1.Name) ? "Skill 1" : c.Skill1.Name;
            }

            if (skill2NameText != null)
            {
                skill2NameText.text = string.IsNullOrEmpty(c.Skill2.Name) ? "Skill 2" : c.Skill2.Name;
            }

            if (passiveNameText != null)
            {
                passiveNameText.text = string.IsNullOrEmpty(c.Passive.Name) ? "Passive" : c.Passive.Name;
            }

            if (passiveDescText != null)
            {
                passiveDescText.text = string.IsNullOrEmpty(c.Passive.Desc) ? string.Empty : c.Passive.Desc;
            }
            // passiveIcon: CharacterData.Passive tidak membawa Sprite — biarkan
            // placeholder bootstrap (background), tidak ditimpa.
        }
    }
}
