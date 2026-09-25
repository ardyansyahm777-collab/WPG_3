using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using WpgGame.Player;

namespace WpgGame.UI
{
    /// <summary>
    /// Character Select untuk Main Menu.
    /// Alur: HeroesButton -> CardSelectPanel (grid) -> klik kartu -> ShowcasePanel
    /// (Tabs STAT/SKILL/PASIF + hero + detail ScrollRect) -> SELECT&amp;PLAY simpan
    /// PlayerPrefs "wpg3_hero" lalu LoadScene "Main". Locked ditolak (no save/load).
    /// GANTI = kembali ke grid. Back/Cancel = kembali ke MainMenu.
    /// Hanya baca CharacterData + PlayerPrefs + SceneManager. Tanpa manager internal.
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        public const string HeroPrefsKey = "wpg3_hero";

        public enum HeroTab
        {
            Stat = 0,
            Skill = 1,
            Passive = 2
        }

        [Header("Data (ScriptableObject dari WpgGame.Player)")]
        [SerializeField] private CharacterData[] heroes;

        [Header("Panels")]
        [SerializeField] private GameObject cardPanel;
        [SerializeField] private GameObject showcasePanel;
        [SerializeField] private Transform cardGrid;
        [SerializeField] private CharacterCard cardPrefab;

        [Header("MainMenu link (return-focus saja, toggle milik MainMenuController)")]
        [SerializeField] private Button heroesButton;
        [SerializeField] private GameObject mainMenuPanel;

        [Header("Tabs (STAT / SKILL / PASIF)")]
        [SerializeField] private Button tabStatButton;
        [SerializeField] private Button tabSkillButton;
        [SerializeField] private Button tabPassiveButton;

        [Header("Showcase Center")]
        [SerializeField] private TMP_Text heroNameText;
        [SerializeField] private TMP_Text heroRoleText;
        [SerializeField] private Image heroPortraitImage;
        [SerializeField] private TMP_Text heroPortraitFallbackText;

        [Header("Showcase Right (ScrollRect Content)")]
        [SerializeField] private Transform detailContent;
        [Tooltip("Template baris stat (prefab aset, edit visual di Project bukan di hierarchy play-mode).")]
        [SerializeField] private GameObject statRowPrefab;

        [Tooltip("Template baris paragraf skill/pasif. Boleh kosong -> pakai StatRow.")]
        [SerializeField] private GameObject paraRowPrefab;

        [Header("Actions (touch 64-96px, assign di scene)")]
        [SerializeField] private Button selectPlayButton;
        [SerializeField] private Button gantiButton;
        [SerializeField] private Button backButton;

        [Tooltip("Tombol tutup di CardSelectPanel (touch). Boleh kosong -> tutup via Cancel/Heroes.")]
        [SerializeField] private Button cardCloseButton;

        [Header("Input bawaan (drag dari InputSystem_Actions)")]
        [Tooltip("Drag action UI/Cancel dari asset bawaan. Back navigasi.")]
        [SerializeField] private InputActionReference cancelAction;

        [Header("State")]
        [SerializeField] private int selectedIndex;
        [SerializeField] private int activeTab;

        private readonly List<CharacterCard> spawnedCards = new List<CharacterCard>();

        /// <summary>
        /// Kartu PERSISTEN: ditaruh manual di hierarchy (CardGrid) agar bisa diedit visual
        /// di edit-mode. Diadopsi otomatis saat RenderCards/ClearCards dan TIDAK PERNAH
        /// dihancurkan (berbeda dari kartu hasil spawn yang dibuang tiap render).
        /// </summary>
        private readonly List<CharacterCard> persistentCards = new List<CharacterCard>();

        /// <summary>
        /// Panel showcase per-hero (Showcase_Nama, anak langsung showcasePanel). Ditemukan
        /// otomatis; bila tak ada, dipakai jalur legacy (Center + DetailScroll bawaan).
        /// </summary>
        private readonly List<Transform> heroShowcases = new List<Transform>();

        private Transform legacyShowcaseCenter;
        private Transform legacyDetailScroll;

        /// <summary>Konten detail aktif untuk baris runtime (diisi tiap RenderShowcase).</summary>
        private Transform activeDetailContent;

        private InputAction runtimeCancel;

        public bool IsShowing =>
            (cardPanel != null && cardPanel.activeSelf) ||
            (showcasePanel != null && showcasePanel.activeSelf);

        public static string SavedHeroId =>
            PlayerPrefs.GetString(HeroPrefsKey, string.Empty);

        private void Awake()
        {
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, (heroes?.Length ?? 1) - 1));
            activeTab = Mathf.Clamp(activeTab, 0, 2);

            if (cardPrefab != null && cardGrid != null && cardPrefab.transform.IsChildOf(cardGrid))
            {
                cardPrefab.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (tabStatButton != null)
            {
                tabStatButton.onClick.AddListener(() => OnTab(0));
            }

            if (tabSkillButton != null)
            {
                tabSkillButton.onClick.AddListener(() => OnTab(1));
            }

            if (tabPassiveButton != null)
            {
                tabPassiveButton.onClick.AddListener(() => OnTab(2));
            }

            if (selectPlayButton != null)
            {
                selectPlayButton.onClick.AddListener(SelectAndPlay);
            }

            if (gantiButton != null)
            {
                gantiButton.onClick.AddListener(OnGantiClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (cardCloseButton != null)
            {
                cardCloseButton.onClick.AddListener(OnBackClicked);
            }

            if (cancelAction != null)
            {
                cancelAction.action.Enable();
                cancelAction.action.performed += HandleCancelPerformed;
            }
            else
            {
                // Fallback runtime agar navigasi Back tetap jalan tanpa wiring manual
                // (maupun di build). Escape = Cancel ala UI map.
                if (runtimeCancel == null)
                {
                    runtimeCancel = new InputAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
                }

                runtimeCancel.Enable();
                runtimeCancel.performed += HandleCancelPerformed;
            }

            RestoreSavedSelection();

            if (cardPanel != null && showcasePanel != null)
            {
                bool show = IsShowing;
                if (!show)
                {
                    cardPanel.SetActive(false);
                    showcasePanel.SetActive(false);
                }
            }
        }

        private void OnDisable()
        {
            if (tabStatButton != null)
            {
                tabStatButton.onClick.RemoveAllListeners();
            }

            if (tabSkillButton != null)
            {
                tabSkillButton.onClick.RemoveAllListeners();
            }

            if (tabPassiveButton != null)
            {
                tabPassiveButton.onClick.RemoveAllListeners();
            }

            if (selectPlayButton != null)
            {
                selectPlayButton.onClick.RemoveAllListeners();
            }

            if (gantiButton != null)
            {
                gantiButton.onClick.RemoveAllListeners();
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
            }

            if (cardCloseButton != null)
            {
                cardCloseButton.onClick.RemoveAllListeners();
            }

            if (cancelAction != null)
            {
                cancelAction.action.performed -= HandleCancelPerformed;
                cancelAction.action.Disable();
            }

            if (runtimeCancel != null)
            {
                runtimeCancel.performed -= HandleCancelPerformed;
                runtimeCancel.Disable();
            }

            ClearCards();
        }

        // ---------- Public API (dipanggil MainMenuController + Button) ----------

        public void ShowSelect()
        {
            activeTab = Mathf.Clamp(activeTab, 0, 2);
            if (heroes == null || heroes.Length == 0)
            {
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, heroes.Length - 1);

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (cardPanel != null)
            {
                cardPanel.SetActive(true);
            }

            if (showcasePanel != null)
            {
                showcasePanel.SetActive(false);
            }

            RenderCards();
            FocusFirstCard();
        }

        public void HideSelect()
        {
            if (cardPanel != null)
            {
                cardPanel.SetActive(false);
            }

            if (showcasePanel != null)
            {
                showcasePanel.SetActive(false);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }

            if (heroesButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(heroesButton.gameObject);
            }
        }

        /// <summary>
        /// Bersihkan sisa objek runtime (kartu spawn + baris detail) dan kembalikan
        /// ke MainMenu. Dipakai tooling validasi agar scene tersimpan bersih.
        /// </summary>
        public void CleanupRuntime()
        {
            ClearCards();
            ClearDetails();
            HideSelect();
        }

        public void ToggleSelect()
        {
            if (IsShowing)
            {
                HideSelect();
            }
            else
            {
                ShowSelect();
            }
        }

        /// <summary>
        /// Render kartu hero ke grid. Kartu persisten (edit manual di hierarchy) dipakai ulang
        /// dan hanya di-Bind ulang; kartu baru di-spawn dari template bila kurang. Kartu persisten
        /// yang tak kebagian hero disembunyikan (tidak dihancurkan) agar edit-an user aman.
        /// </summary>
        public void RenderCards()
        {
            if (cardGrid == null || cardPrefab == null || heroes == null || heroes.Length == 0)
            {
                Debug.LogError("[CharSelect] RenderCards batal: cardGrid/cardPrefab/heroes belum di-wire di Inspector.",
                    this);
                return;
            }

            ClearCards();

            if (cardPrefab.transform.IsChildOf(cardGrid))
            {
                cardPrefab.gameObject.SetActive(false);
                cardPrefab.transform.SetAsFirstSibling();
            }

            var used = new HashSet<CharacterCard>();
            int order = 0;
            for (int i = 0; i < heroes.Length; i++)
            {
                CharacterData data = heroes[i];
                if (data == null)
                {
                    continue;
                }

                // 1) Kartu yang namanya sudah cocok (stabil antar render).
                // 2) Kartu persisten pertama yang belum dipakai (urutan hierarchy).
                // 3) Spawn baru dari template (fallback).
                CharacterCard card = FindPersistentByName("Card_" + ResolveHeroTitle(data, i), used);
                if (card == null)
                {
                    for (int p = 0; p < persistentCards.Count; p++)
                    {
                        CharacterCard cand = persistentCards[p];
                        if (cand != null && !used.Contains(cand))
                        {
                            card = cand;
                            break;
                        }
                    }
                }

                if (card == null)
                {
                    card = Instantiate(cardPrefab, cardGrid);
                    WpgGame.Core.RuntimeSpawnTag.Tag(card.gameObject, "WpgGame.UI.CharacterSelectController.RenderCards", "CardTemplate");
                }

                card.gameObject.SetActive(true);
                card.transform.SetSiblingIndex(order + 1);
                order++;

                int captured = i;
                card.Bind(data, captured == selectedIndex, () => SetSelected(captured));
                spawnedCards.Add(card);
                used.Add(card);
            }

            for (int p = 0; p < persistentCards.Count; p++)
            {
                CharacterCard c = persistentCards[p];
                if (c != null && !used.Contains(c))
                {
                    c.gameObject.SetActive(false);
                }
            }
        }

        public void RenderShowcase()
        {
            if (heroes == null || heroes.Length == 0)
            {
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, heroes.Length - 1);
            activeTab = Mathf.Clamp(activeTab, 0, 2);
            CharacterData hero = heroes[selectedIndex];
            if (hero == null)
            {
                return;
            }

            string title = !string.IsNullOrEmpty(hero.DisplayName)
                ? hero.DisplayName
                : ResolveHeroId(hero, selectedIndex);

            // Showcase per-hero bila ada (fallback ke refs legacy bila tidak ada).
            DiscoverShowcases();
            Transform showcase = FindShowcasePanel(title);
            Transform content = ResolveShowcaseContent(showcase);
            TMP_Text nameT = ResolveShowcaseText(showcase, "Center/HeroNameText", heroNameText);
            TMP_Text roleT = ResolveShowcaseText(showcase, "Center/HeroRoleText", heroRoleText);
            Image portraitImg = ResolveShowcaseImage(showcase, "Center/HeroPortraitImage", heroPortraitImage);
            TMP_Text portraitT = ResolveShowcaseText(showcase, "Center/HeroPortraitText", heroPortraitFallbackText);
            ShowOnlyShowcase(showcase);
            activeDetailContent = content;

            if (nameT != null)
            {
                nameT.text = title;
            }

            if (roleT != null)
            {
                roleT.text = string.IsNullOrEmpty(hero.Role) ? "-" : hero.Role;
            }

            if (portraitImg != null)
            {
                // API baru tidak punya Sprite portrait: sembunyikan Image,
                // tampilkan PortraitText (inisial) sebagai placeholder TMP.
                portraitImg.enabled = false;
            }

            if (portraitT != null)
            {
                string portrait = !string.IsNullOrEmpty(hero.PortraitText)
                    ? hero.PortraitText
                    : (title.Length > 0 ? title.Substring(0, 1).ToUpper() : string.Empty);
                portraitT.text = portrait;
                portraitT.gameObject.SetActive(true);
            }

            ClearDetails();

            if (activeTab == (int)HeroTab.Stat)
            {
                AddRow("HP", hero.HP.ToString("0"));
                AddRow("Physical Attack", hero.PhysicalAttack.ToString("0.##"));
                AddRow("Physical Defense", hero.PhysicalDefense.ToString("0.##"));
                AddRow("Attack Speed", hero.AttackSpeed.ToString("0.##"));
                AddRow("Critical Chance", hero.CritChance.ToString("0.##") + "%");
                AddRow("Mana", hero.Mana.ToString("0"));
                AddRow("Magic Power", hero.MagicPower.ToString("0.##"));
                AddRow("Magical Defense", hero.MagicalDefense.ToString("0.##"));
                AddRow("Cooldown Reduction", hero.CooldownReduction.ToString("0.##") + "%");
                AddRow("Movement Speed", hero.MovementSpeed.ToString("0.##"));
                AddRow("HP Regen", hero.HPRegen.ToString("0.##") + "/s");
                AddRow("Physical PEN", hero.PhysicalPEN.ToString("0.##"));
                AddRow("Lifesteal", hero.Lifesteal.ToString("0.##") + "%");
                AddRow("Jangkauan Basic Attack", hero.BasicAttackRange.ToString("0.##"));
                AddRow("Critical Damage", hero.CritDamage.ToString("0.##") + "%");
                AddRow("Efek Pemulihan", hero.HealEffect.ToString("0.##") + "%");
                AddRow("Pemulihan Mana", hero.ManaRegen.ToString("0.##") + "/s");
                AddRow("Magical PEN", hero.MagicalPEN.ToString("0.##"));
                AddRow("Spell Vamp", hero.SpellVamp.ToString("0.##") + "%");
                AddRow("Pemulihan Diterima", hero.IncomingHeal.ToString("0.##") + "%");
                if (!hero.IsUnlocked)
                {
                    AddParagraph("Status", "LOCKED: pilih hero lain atau buka kuncinya.", 120f);
                }
            }
            else if (activeTab == (int)HeroTab.Skill)
            {
                AddSkill(1, hero.Skill1.Name, hero.Skill1.Desc, hero.Skill1.Cooldown);
                AddSkill(2, hero.Skill2.Name, hero.Skill2.Desc, hero.Skill2.Cooldown);
                AddSkill(3, hero.Skill3.Name, hero.Skill3.Desc, hero.Skill3.Cooldown);
            }
            else
            {
                string pasifTitle = string.IsNullOrEmpty(hero.Passive.Name) ? "Pasif" : hero.Passive.Name;
                string pasifBody = string.IsNullOrEmpty(hero.Passive.Desc) ? "-" : hero.Passive.Desc;
                AddParagraph(pasifTitle, pasifBody, 180f);
            }

            RefreshTabs();

            if (selectPlayButton != null)
            {
                selectPlayButton.interactable = hero.IsUnlocked;
            }
        }

        public void OnTab(int tabIndex)
        {
            activeTab = Mathf.Clamp(tabIndex, 0, 2);
            RenderShowcase();
        }

        public void SetSelected(int index)
        {
            if (heroes == null || heroes.Length == 0)
            {
                return;
            }

            if (index < 0 || index >= heroes.Length || heroes[index] == null)
            {
                return;
            }

            selectedIndex = index;
            activeTab = (int)HeroTab.Stat;

            RenderCards();
            RenderShowcase();

            if (cardPanel != null)
            {
                cardPanel.SetActive(false);
            }

            if (showcasePanel != null)
            {
                showcasePanel.SetActive(true);
            }

            if (selectPlayButton != null && selectPlayButton.interactable)
            {
                EventSystem.current?.SetSelectedGameObject(selectPlayButton.gameObject);
            }
            else if (gantiButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(gantiButton.gameObject);
            }
        }

        public void SelectAndPlay()
        {
            if (heroes == null || heroes.Length == 0)
            {
                return;
            }

            if (selectedIndex < 0 || selectedIndex >= heroes.Length)
            {
                return;
            }

            CharacterData hero = heroes[selectedIndex];
            if (hero == null)
            {
                return;
            }

            if (!hero.IsUnlocked)
            {
                return;
            }

            PlayerPrefs.SetString(HeroPrefsKey, ResolveHeroId(hero, selectedIndex));
            PlayerPrefs.Save();
            GameplaySetup.PendingCharacter = hero;
            SceneManager.LoadScene("Main");
        }

        // ---------- Internal ----------

        private void OnGantiClicked()
        {
            if (cardPanel != null)
            {
                cardPanel.SetActive(true);
            }

            if (showcasePanel != null)
            {
                showcasePanel.SetActive(false);
            }

            RenderCards();
            FocusFirstCard();
        }

        private void OnBackClicked()
        {
            HideSelect();
        }

        private void HandleCancelPerformed(InputAction.CallbackContext _)
        {
            if (!IsShowing)
            {
                return;
            }

            bool showcaseOpen = showcasePanel != null && showcasePanel.activeSelf;
            if (showcaseOpen)
            {
                OnGantiClicked();
            }
            else
            {
                HideSelect();
            }
        }

        private void RestoreSavedSelection()
        {
            if (heroes == null || heroes.Length == 0)
            {
                selectedIndex = 0;
                return;
            }

            string saved = PlayerPrefs.GetString(HeroPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(saved))
            {
                selectedIndex = Mathf.Clamp(selectedIndex, 0, heroes.Length - 1);
                return;
            }

            for (int i = 0; i < heroes.Length; i++)
            {
                if (heroes[i] != null && ResolveHeroId(heroes[i], i) == saved)
                {
                    selectedIndex = i;
                    return;
                }
            }
        }

        private void FocusFirstCard()
        {
            if (spawnedCards.Count > 0 && spawnedCards[0] != null && spawnedCards[0].CardButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(spawnedCards[0].CardButton.gameObject);
            }
            else if (tabStatButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(tabStatButton.gameObject);
            }
        }

        private static string ResolveHeroId(CharacterData hero, int fallbackIndex)
        {
            if (hero == null)
            {
                return "hero_" + fallbackIndex;
            }

            if (!string.IsNullOrEmpty(hero.Id))
            {
                return hero.Id;
            }

            if (!string.IsNullOrEmpty(hero.DisplayName))
            {
                return hero.DisplayName;
            }

            return "hero_" + fallbackIndex;
        }

        /// <summary>Judul kartu versi Bind (DisplayName else Id else "Hero").</summary>
        private static string ResolveHeroTitle(CharacterData hero, int fallbackIndex)
        {
            if (hero == null)
            {
                return "Hero";
            }

            if (!string.IsNullOrEmpty(hero.DisplayName))
            {
                return hero.DisplayName;
            }

            if (!string.IsNullOrEmpty(hero.Id))
            {
                return hero.Id;
            }

            return "Hero";
        }

        /// <summary>
        /// Cari kartu persisten yang namanya sudah cocok dan belum dipakai render ini.
        /// Membuat mapping hero↔kartu stabil antar render (tidak tertukar).
        /// </summary>
        private CharacterCard FindPersistentByName(string cardName, HashSet<CharacterCard> used)
        {
            if (string.IsNullOrEmpty(cardName))
            {
                return null;
            }

            for (int i = 0; i < persistentCards.Count; i++)
            {
                CharacterCard cand = persistentCards[i];
                if (cand != null && !used.Contains(cand) && cand.gameObject.name == cardName)
                {
                    return cand;
                }
            }

            return null;
        }

        /// <summary>
        /// Pindai anak langsung showcasePanel: kumpulkan Showcase_* dan catat Center /
        /// DetailScroll legacy (fallback bila panel per-hero tidak ada).
        /// </summary>
        private void DiscoverShowcases()
        {
            heroShowcases.Clear();
            legacyShowcaseCenter = null;
            legacyDetailScroll = null;
            if (showcasePanel == null)
            {
                return;
            }

            Transform root = showcasePanel.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (child.name.StartsWith("Showcase_"))
                {
                    heroShowcases.Add(child);
                }
                else if (child.name == "Center")
                {
                    legacyShowcaseCenter = child;
                }
                else if (child.name == "DetailScroll")
                {
                    legacyDetailScroll = child;
                }
            }
        }

        /// <summary>Cari panel Showcase_Judul untuk hero aktif (null bila belum dibuat).</summary>
        private Transform FindShowcasePanel(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return null;
            }

            string want = "Showcase_" + title;
            for (int i = 0; i < heroShowcases.Count; i++)
            {
                Transform t = heroShowcases[i];
                if (t != null && t.name == want)
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>
        /// Tampilkan hanya panel hero aktif. Bila panel per-hero ada, Center + DetailScroll
        /// legacy disembunyikan (isinya basi); bila tidak ada, legacy dipastikan tampil.
        /// Tombol/tab/bingkai shared tidak disentuh.
        /// </summary>
        private void ShowOnlyShowcase(Transform active)
        {
            for (int i = 0; i < heroShowcases.Count; i++)
            {
                Transform t = heroShowcases[i];
                if (t != null)
                {
                    t.gameObject.SetActive(t == active);
                }
            }

            bool legacy = active == null;
            if (legacyShowcaseCenter != null)
            {
                legacyShowcaseCenter.gameObject.SetActive(legacy);
            }

            if (legacyDetailScroll != null)
            {
                legacyDetailScroll.gameObject.SetActive(legacy);
            }
        }

        private static TMP_Text ResolveShowcaseText(Transform panel, string path, TMP_Text fallback)
        {
            if (panel != null)
            {
                Transform t = panel.Find(path);
                if (t != null)
                {
                    TMP_Text tmp = t.GetComponent<TMP_Text>();
                    if (tmp != null)
                    {
                        return tmp;
                    }
                }
            }

            return fallback;
        }

        private static Image ResolveShowcaseImage(Transform panel, string path, Image fallback)
        {
            if (panel != null)
            {
                Transform t = panel.Find(path);
                if (t != null)
                {
                    Image img = t.GetComponent<Image>();
                    if (img != null)
                    {
                        return img;
                    }
                }
            }

            return fallback;
        }

        private Transform ResolveShowcaseContent(Transform panel)
        {
            if (panel != null)
            {
                Transform dc = panel.Find("DetailScroll/Viewport/DetailContent");
                if (dc != null)
                {
                    return dc;
                }
            }

            return detailContent;
        }

        private void AddSkill(int slot, string name, string desc, float cooldown)
        {
            string title = "[Tombol " + slot + "] " + (string.IsNullOrEmpty(name) ? "Skill " + slot : name);
            string body = (string.IsNullOrEmpty(desc) ? "-" : desc)
                + "\nCooldown: " + cooldown.ToString("0.##") + "s";
            AddParagraph(title, body, 240f);
        }

        private void RefreshTabs()
        {
            SetTabVisual(tabStatButton, activeTab == (int)HeroTab.Stat);
            SetTabVisual(tabSkillButton, activeTab == (int)HeroTab.Skill);
            SetTabVisual(tabPassiveButton, activeTab == (int)HeroTab.Passive);
        }

        private static void SetTabVisual(Button tabButton, bool active)
        {
            if (tabButton == null || tabButton.targetGraphic == null)
            {
                return;
            }

            tabButton.targetGraphic.color = active
                ? new Color(0.22f, 0.74f, 0.97f, 1f)
                : new Color(1f, 1f, 1f, 0.1f);
        }

        /// <summary>
        /// Sapu grid: template dimatikan; kartu buatan user diadopsi sebagai persisten (TIDAK dihancurkan);
        /// sampah non-kartu dibuang. Lalu hancurkan HANYA kartu hasil spawn sebelumnya.
        /// Aman dipanggil berulang (OnDisable/RenderCards/CleanupRuntime).
        /// </summary>
        private void ClearCards()
        {
            if (cardGrid != null)
            {
                // Dua pass tanpa mutasi saat iterasi: adopsi maju by-index agar urutan
                // sibling deterministik, lalu buang sampah non-kartu sekaligus.
                // (DestroyImmediate di edit-mode menggeser sibling — forward + hapus
                // langsung akan skip anak.)
                var junk = new List<GameObject>();
                for (int i = 0; i < cardGrid.childCount; i++)
                {
                    Transform child = cardGrid.GetChild(i);
                    if (child == null)
                    {
                        continue;
                    }

                    if (cardPrefab != null && child.gameObject == cardPrefab.gameObject)
                    {
                        child.gameObject.SetActive(false);
                        continue;
                    }

                    CharacterCard cc = child.GetComponent<CharacterCard>();
                    if (cc == null)
                    {
                        junk.Add(child.gameObject);
                        continue;
                    }

                    if (!spawnedCards.Contains(cc) && !persistentCards.Contains(cc))
                    {
                        persistentCards.Add(cc);
                    }
                }

                for (int i = 0; i < junk.Count; i++)
                {
                    DestroyNmi(junk[i]);
                }

                for (int i = persistentCards.Count - 1; i >= 0; i--)
                {
                    if (persistentCards[i] == null)
                    {
                        persistentCards.RemoveAt(i);
                    }
                }
            }

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                CharacterCard c = spawnedCards[i];
                if (c == null || persistentCards.Contains(c))
                {
                    continue;
                }

                DestroyNmi(c.gameObject);
            }

            spawnedCards.Clear();
        }

        /// <summary>
        /// Bersihkan SEMUA konten detail (legacy + tiap panel per-hero) agar tidak ada baris
        /// basi saat ganti hero/tab. Dipanggil tiap RenderShowcase + CleanupRuntime.
        /// </summary>
        private void ClearDetails()
        {
            ClearContent(detailContent);
            for (int i = 0; i < heroShowcases.Count; i++)
            {
                Transform t = heroShowcases[i];
                if (t == null)
                {
                    continue;
                }

                Transform dc = t.Find("DetailScroll/Viewport/DetailContent");
                ClearContent(dc);
            }
        }

        private void ClearDetail()
        {
            ClearContent(detailContent);
        }

        private static void ClearContent(Transform content)
        {
            if (content == null)
            {
                return;
            }

            // Mundur by-index: aman untuk DestroyImmediate.
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Transform child = content.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                DestroyNmi(child.gameObject);
            }
        }

        /// <summary>Konten tujuan baris runtime: aktif per-hero, fallback ke legacy.</summary>
        private Transform TargetContent()
        {
            return activeDetailContent != null ? activeDetailContent : detailContent;
        }

        /// <summary>
        /// Destroy aman dua konteks: play pakai Destroy (akhir frame),
        /// edit/tooling pakai DestroyImmediate (tanpa error edit-mode).
        /// </summary>
        private static void DestroyNmi(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        private void AddRow(string title, string value)
        {
            Transform content = TargetContent();
            if (content == null)
            {
                return;
            }

            if (statRowPrefab != null)
            {
                GameObject row = Instantiate(statRowPrefab, content);
                row.SetActive(true);
                row.name = "Row_" + title;
                WpgGame.Core.RuntimeSpawnTag.Tag(row, "WpgGame.UI.CharacterSelectController.AddRow", "Assets/Prefabs/UI/StatRow.prefab");

                TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length >= 2)
                {
                    texts[0].text = title;
                    texts[1].text = value;
                }
                else if (texts.Length == 1)
                {
                    texts[0].text = title + ": " + value;
                }

                // Tinggi prefab dijaga 56 (identik jalur kode) agar tampilan tak berubah.
                EnsureTouchHeight(row, 56f);
                return;
            }

            // Baris 1 kolom fixed-height: label muted + nilai tebal sebaris,
            // tanpa nested layout (anti-fragile terhadap preferred-size TMP).
            GameObject go = new GameObject("Row_" + title, typeof(RectTransform));
            go.transform.SetParent(content, false);
            WpgGame.Core.RuntimeSpawnTag.Tag(go, "WpgGame.UI.CharacterSelectController.AddRow (code fallback)");

            TMP_Text tmp = StretchText(go,
                "<color=#94A3B8>" + title + ":</color>  <b>" + value + "</b>",
                24, TextAlignmentOptions.Left);
            tmp.richText = true;
            FixRowHeight(go, 56f);
        }

        private void AddParagraph(string title, string body, float bodyHeight)
        {
            Transform content = TargetContent();
            if (content == null)
            {
                return;
            }

            // Prefab paragraf diutamakan; fallback ke StatRow lalu jalur kode.
            GameObject proto = paraRowPrefab != null ? paraRowPrefab : statRowPrefab;
            if (proto != null)
            {
                GameObject row = Instantiate(proto, content);
                row.SetActive(true);
                row.name = "Row_" + title;
                WpgGame.Core.RuntimeSpawnTag.Tag(row, "WpgGame.UI.CharacterSelectController.AddParagraph", "Assets/Prefabs/UI/ParaRow.prefab");

                TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length >= 2)
                {
                    texts[0].text = title;
                    texts[1].text = body;
                }
                else if (texts.Length == 1)
                {
                    texts[0].text = title + "\n" + body;
                }

                EnsureTouchHeight(row);
                return;
            }

            GameObject go = new GameObject("Row_" + title, typeof(RectTransform));
            go.transform.SetParent(content, false);
            WpgGame.Core.RuntimeSpawnTag.Tag(go, "WpgGame.UI.CharacterSelectController.AddParagraph (code fallback)");

            // Satu TMP full-rect fixed-height: judul aksen cyan + isi. Tinggi dikunci agar
            // tidak bergantung preferred-size (lihat FixRowHeight).
            TMP_Text tmp = StretchText(go,
                "<color=#38BDF8><b>" + title + "</b></color>\n" + body,
                24, TextAlignmentOptions.TopLeft);
            tmp.richText = true;
            FixRowHeight(go, bodyHeight);
        }

        /// <summary>
        /// TMP full-stretch di dalam baris + overflow Ellipsis (tak pernah overlap).
        /// </summary>
        private static TMP_Text StretchText(GameObject row, string text, int size, TextAlignmentOptions align)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(row.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 6f);
            rt.offsetMax = new Vector2(-8f, -6f);

            TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>
        /// Kunci tinggi baris (min + preferred sama) sehingga parent VerticalLayoutGroup
        /// menumpuk deterministik tanpa menunggu preferred-size TMP.
        /// </summary>
        private static void FixRowHeight(GameObject row, float height)
        {
            if (row == null)
            {
                return;
            }

            var layout = row.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = row.AddComponent<LayoutElement>();
            }

            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        /// <summary>Sel TMP non-raycast dengan wrapping kata normal.</summary>
        private static TMP_Text CreateCell(GameObject parent, string name, string text, int size, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void EnsureTouchHeight(GameObject row, float minHeight = 72f)
        {
            if (row == null)
            {
                return;
            }

            var layout = row.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = row.AddComponent<LayoutElement>();
            }

            if (layout.minHeight < minHeight)
            {
                layout.minHeight = minHeight;
            }
        }
    }
}
