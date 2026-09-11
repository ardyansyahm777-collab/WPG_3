#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using WpgGame.Player;

namespace WpgGame.UI
{
    /// <summary>
    /// Tooling sekali-pakai (lead): membangun hierarchy Character Select di scene
    /// "Main Menu" mengikuti MainMenuController + CharacterSelectController.
    /// Menu: Tools/WPG_3/Build Character Select UI. Aman di-run ulang (idempoten:
    /// object yang sudah ada dipakai ulang, tidak diduplikasi).
    /// </summary>
    public static class CharacterSelectBootstrap
    {
        private const string SceneName = "Main Menu";

        private static readonly string[] HeroPaths =
        {
            "Assets/Data/Characters/Aelindra.asset",
            "Assets/Data/Characters/Bram.asset",
            "Assets/Data/Characters/Circe.asset",
            "Assets/Data/Characters/Dain.asset",
        };

        // Palet fable.html (dark navy + cyan + gold). Satu sumber warna agar
        // Build/Reskin idempoten dan konvergen ke tampilan yang sama.
        private const string FableBg = "#0F172A";
        private const string FablePanel = "#1E293B";
        private const string FableAccent = "#38BDF8";
        private const string FableGold = "#FBBF24";
        private const string FableText = "#F1F5F9";
        private const string FableMuted = "#94A3B8";
        private const string FableDanger = "#EF4444";

        // Prefab template baris detail (WYSIWYG di Project, dipakai runtime).
        // Isi DetailContent di hierarchy SELALU kosong di edit-mode — baris lahir
        // dari kode + prefab ini, jadi jangan edit anak DetailContent manual.
        private const string StatRowPrefabPath = "Assets/Prefabs/UI/StatRow.prefab";
        private const string ParaRowPrefabPath = "Assets/Prefabs/UI/ParaRow.prefab";

        private static Color Hex(string html)
        {
            Color c;
            if (ColorUtility.TryParseHtmlString(html, out c))
            {
                return c;
            }

            return Color.white;
        }

        [MenuItem("Tools/WPG_3/Build Character Select UI")]
        public static void Build()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != SceneName)
            {
                Debug.LogError("[CharSelect] Scene aktif bukan 'Main Menu': " + scene.name);
                return;
            }

            var menuUI = GameObject.Find("MenuUI");
            var mainMenu = GameObject.Find("MenuUI/MainMenu");
            // Tombol bisa sudah pindah ke ButtonRow dari run Build/Reskin sebelumnya
            // (GameObject.Find tidak menemukan objek inactive — cari di dua path).
            var playBtn = GameObject.Find("MenuUI/MainMenu/ButtonRow/PlayButton");
            if (playBtn == null)
            {
                playBtn = GameObject.Find("MenuUI/MainMenu/PlayButton");
            }

            if (menuUI == null || mainMenu == null || playBtn == null)
            {
                Debug.LogError("[CharSelect] Struktur MenuUI/MainMenu/PlayButton tidak ditemukan.");
                return;
            }

            var playButton = playBtn.GetComponent<Button>();
            var playLabel = playBtn.GetComponentInChildren<TMP_Text>(true);

            // 1. Baris tombol main menu (PLAY/HERO/SHOP/EVENT/SET) + chrome fable.
            // Row-aware: cari HeroesButton di dua lokasi agar re-run tidak duplikasi.
            var heroesBtn = GameObject.Find("MenuUI/MainMenu/ButtonRow/HeroesButton");
            if (heroesBtn == null)
            {
                heroesBtn = GameObject.Find("MenuUI/MainMenu/HeroesButton");
            }

            if (heroesBtn == null)
            {
                heroesBtn = FindOrCloneButton(mainMenu.transform, playBtn, "HeroesButton", "HERO");
            }
            EnsureMainMenuRow(mainMenu.transform, playBtn, heroesBtn);
            StyleMainMenuChrome(mainMenu.transform);

            // 2. Panels (palet fable: navy gelap, bukan krem).
            var cardPanel = FindOrCreatePanel(menuUI.transform, "CardSelectPanel",
                Hex(FableBg));
            var showcasePanel = FindOrCreatePanel(menuUI.transform, "ShowcasePanel",
                Hex(FableBg));

            // 3. Card panel isi.
            AddTitle(cardPanel.transform, "CardTitle", "PILIH HERO",
                Hex(FableText), 64);
            var cardGrid = FindOrCreate(cardPanel.transform, "CardGrid", typeof(RectTransform));
            var grid = GetOrAdd<GridLayoutGroup>(cardGrid);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(280f, 620f);
            grid.spacing = new Vector2(24f, 24f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(24, 24, 24, 24);
            var gridRt = cardGrid.GetComponent<RectTransform>();
            AnchorStretchCenter(gridRt, new Vector2(1400f, 700f), new Vector2(0f, 46f));
            var cardClose = FindOrCloneButton(cardPanel.transform, playBtn, "CardCloseButton", "TUTUP");
            StyleButton(cardClose, new Color(1f, 1f, 1f, 0.08f), Hex(FableText), 28, true);
            AnchorBottom(cardClose.GetComponent<RectTransform>(), new Vector2(0f, 90f));

            // Template kartu (inactive, dipakai Instantiate oleh controller).
            var template = BuildCardTemplate(cardGrid.transform, playButton, playLabel);

            // 4. Showcase isi.
            var tabs = FindOrCreate(showcasePanel.transform, "LeftTabs", typeof(RectTransform));
            var tabsLayout = GetOrAdd<VerticalLayoutGroup>(tabs);
            tabsLayout.spacing = 16f;
            tabsLayout.childAlignment = TextAnchor.MiddleCenter;
            tabsLayout.childControlWidth = true;
            tabsLayout.childControlHeight = false;
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.childForceExpandHeight = false;
            tabsLayout.padding = new RectOffset(12, 12, 24, 24);
            var tabsBg = GetOrAdd<Image>(tabs);
            tabsBg.color = new Color(0f, 0f, 0f, 0.2f);
            AnchorLeft(tabs.GetComponent<RectTransform>(), 200f);
            var tabStat = FindOrCloneButton(tabs.transform, playBtn, "TabStatButton", "STAT");
            var tabSkill = FindOrCloneButton(tabs.transform, playBtn, "TabSkillButton", "SKILL");
            var tabPasif = FindOrCloneButton(tabs.transform, playBtn, "TabPassiveButton", "PASIF");
            foreach (var t in new[] { tabStat, tabSkill, tabPasif })
            {
                var rt = t.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(160f, 96f);
                StyleButton(t, new Color(1f, 1f, 1f, 0.08f), Hex(FableText), 28, true);
            }

            var center = FindOrCreate(showcasePanel.transform, "Center", typeof(RectTransform));
            AnchorCenter(center.GetComponent<RectTransform>(), new Vector2(520f, 800f), new Vector2(-100f, 0f));
            EnforceCenterLayout(center);
            var portrait = AddText(center.transform, "HeroPortraitText", "AE", 150,
                Hex(FableAccent));
            portrait.fontStyle = FontStyles.Bold;
            portrait.overflowMode = TextOverflowModes.Overflow;
            LockHeight(portrait.gameObject, 240f);
            var heroName = AddText(center.transform, "HeroNameText", "Aelindra", 64,
                Hex(FableText));
            heroName.fontStyle = FontStyles.Bold;
            heroName.overflowMode = TextOverflowModes.Overflow;
            LockHeight(heroName.gameObject, 120f);
            var heroRole = AddText(center.transform, "HeroRoleText", "Swift Archer", 36,
                Hex(FableAccent));
            heroRole.overflowMode = TextOverflowModes.Overflow;
            LockHeight(heroRole.gameObject, 70f);
            var selectBtn = FindOrCloneButton(center.transform, playBtn, "SelectPlayButton", "SELECT & PLAY");
            StyleButton(selectBtn, Hex(FableAccent), Hex(FableBg), 32, true);
            LockHeight(selectBtn, 95f);
            var gantiBtn = FindOrCloneButton(center.transform, playBtn, "GantiButton", "GANTI HERO");
            StyleButton(gantiBtn, new Color(1f, 1f, 1f, 0.08f), Hex(FableText), 32, true);
            LockHeight(gantiBtn, 95f);

            var scroll = FindOrCreate(showcasePanel.transform, "DetailScroll", typeof(RectTransform), typeof(ScrollRect));
            AnchorRight(scroll.GetComponent<RectTransform>(), 520f);
            var scrollRect = scroll.GetComponent<ScrollRect>();
            var viewport = FindOrCreate(scroll.transform, "Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            var vpImg = viewport.GetComponent<Image>();
            vpImg.color = Hex(FablePanel);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            var content = FindOrCreate(viewport.transform, "DetailContent", typeof(RectTransform));
            // Jangkar atas: konten tumbuh ke bawah sehingga posisi awal scroll
            // selalu menampilkan baris teratas (bukan tengah/bawah).
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, contentRt.sizeDelta.y);
            var vlg = GetOrAdd<VerticalLayoutGroup>(content);
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(12, 12, 12, 12);
            // WAJIB eksplisit: default VLG tidak menjamin control flags —
            // tanpa ini baris runtime (RectTransform 0x0) collapse jadi strip.
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = GetOrAdd<ContentSizeFitter>(content);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = vpRt;
            scrollRect.content = content.GetComponent<RectTransform>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            var backBtn = FindOrCloneButton(showcasePanel.transform, playBtn, "BackButton", "KEMBALI");
            StyleButton(backBtn, new Color(1f, 1f, 1f, 0.08f), Hex(FableText), 28, true);
            // Pojok kiri-bawah (bukan tengah-bawah) agar tak tabrakan kolom Center.
            var backRt = backBtn.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0f, 0f);
            backRt.anchorMax = new Vector2(0f, 0f);
            backRt.pivot = new Vector2(0f, 0f);
            backRt.anchoredPosition = new Vector2(60f, 40f);
            backRt.sizeDelta = new Vector2(320f, 90f);

            // 5. Controller + wiring.
            var selectGO = FindOrCreate(menuUI.transform, "CharacterSelect", typeof(RectTransform));
            var controller = GetOrAdd<CharacterSelectController>(selectGO);
            var heroes = new CharacterData[HeroPaths.Length];
            for (int i = 0; i < HeroPaths.Length; i++)
            {
                heroes[i] = AssetDatabase.LoadAssetAtPath<CharacterData>(HeroPaths[i]);
                if (heroes[i] == null)
                {
                    Debug.LogError("[CharSelect] Asset tidak ditemukan: " + HeroPaths[i]);
                }
            }

            var so = new SerializedObject(controller);
            SetObj(so, "heroes", heroes);
            SetObj(so, "cardPanel", cardPanel);
            SetObj(so, "showcasePanel", showcasePanel);
            SetObj(so, "cardGrid", cardGrid.transform);
            SetObj(so, "cardPrefab", template.GetComponent<CharacterCard>());
            SetObj(so, "heroesButton", heroesBtn.GetComponent<Button>());
            SetObj(so, "mainMenuPanel", mainMenu);
            SetObj(so, "tabStatButton", tabStat.GetComponent<Button>());
            SetObj(so, "tabSkillButton", tabSkill.GetComponent<Button>());
            SetObj(so, "tabPassiveButton", tabPasif.GetComponent<Button>());
            SetObj(so, "heroNameText", heroName);
            SetObj(so, "heroRoleText", heroRole);
            SetObj(so, "heroPortraitImage", (Object)null);
            SetObj(so, "heroPortraitFallbackText", portrait);
            SetObj(so, "detailContent", content.transform);
            var statRow = EnsureRowPrefab(StatRowPrefabPath, "StatRow", 56f, false);
            var paraRow = EnsureRowPrefab(ParaRowPrefabPath, "ParaRow", 240f, true);
            SetObj(so, "statRowPrefab", statRow);
            SetObj(so, "paraRowPrefab", paraRow);
            SetObj(so, "selectPlayButton", selectBtn.GetComponent<Button>());
            SetObj(so, "gantiButton", gantiBtn.GetComponent<Button>());
            SetObj(so, "backButton", backBtn.GetComponent<Button>());
            SetObj(so, "cardCloseButton", cardClose.GetComponent<Button>());
            SetObj(so, "cancelAction", (Object)null);
            so.ApplyModifiedPropertiesWithoutUndo();

            var mmc = mainMenu.GetComponent<MainMenuController>();
            if (mmc != null)
            {
                var mmSo = new SerializedObject(mmc);
                mmSo.FindProperty("heroesButton").objectReferenceValue = heroesBtn.GetComponent<Button>();
                mmSo.FindProperty("characterSelect").objectReferenceValue = controller;
                mmSo.ApplyModifiedPropertiesWithoutUndo();
            }

            cardPanel.SetActive(false);
            showcasePanel.SetActive(false);

            EditorUtility.SetDirty(controller);
            if (mmc != null)
            {
                EditorUtility.SetDirty(mmc);
            }

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[CharSelect] Build selesai (reskin-fable v2): ButtonRow + CardSelectPanel + ShowcasePanel + wiring.");
        }

        [MenuItem("Tools/WPG_3/Reskin Main Menu (Fable)")]
        public static void ReskinFable()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != SceneName)
            {
                Debug.LogError("[CharSelect] Scene aktif bukan 'Main Menu': " + scene.name);
                return;
            }

            var menuUI = GameObject.Find("MenuUI");
            var mainMenu = GameObject.Find("MenuUI/MainMenu");
            // Tombol bisa sudah pindah ke ButtonRow dari run Build/Reskin sebelumnya.
            var playBtn = GameObject.Find("MenuUI/MainMenu/ButtonRow/PlayButton");
            if (playBtn == null)
            {
                playBtn = GameObject.Find("MenuUI/MainMenu/PlayButton");
            }

            var heroesBtn = GameObject.Find("MenuUI/MainMenu/ButtonRow/HeroesButton");
            if (heroesBtn == null)
            {
                heroesBtn = GameObject.Find("MenuUI/MainMenu/HeroesButton");
            }

            if (menuUI == null || mainMenu == null || playBtn == null || heroesBtn == null)
            {
                Debug.LogError("[CharSelect] Struktur MenuUI/MainMenu/PlayButton/HeroesButton tidak ditemukan.");
                return;
            }

            EnsureMainMenuRow(mainMenu.transform, playBtn, heroesBtn);
            StyleMainMenuChrome(mainMenu.transform);

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[CharSelect] Reskin Fable selesai: ButtonRow 5 tombol + chrome navy.");
        }

        /// <summary>
        /// Baris 5 tombol main menu (PLAY/HERO fungsional; SHOP/EVENT/SET hiasan
        /// disabled + badge SOON). Idempoten: objek dipakai ulang, urutan dijaga.
        /// </summary>
        private static void EnsureMainMenuRow(Transform mainMenu, GameObject playBtn, GameObject heroesBtn)
        {
            var rowT = mainMenu.Find("ButtonRow");
            GameObject row = rowT != null ? rowT.gameObject
                : new GameObject("ButtonRow", typeof(RectTransform));
            if (rowT == null)
            {
                row.transform.SetParent(mainMenu, false);
            }

            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1100f, 190f);
            rt.anchoredPosition = new Vector2(0f, -60f);

            var hlg = GetOrAdd<HorizontalLayoutGroup>(row);
            hlg.spacing = 24f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(0, 0, 0, 0);

            MoveToRow(row.transform, playBtn, 0);
            var playLabel = playBtn.GetComponentInChildren<TMP_Text>(true);
            if (playLabel != null)
            {
                playLabel.text = "PLAY";
                // SENGAJA tanpa FloatingObject: PLAY tetap tanpa layang
                // (keputusan user) — jangan pasang animasi di sini agar Build
                // tidak memasangnya kembali secara diam-diam.
            }

            MoveToRow(row.transform, heroesBtn, 1);
            var heroLabel = heroesBtn.GetComponentInChildren<TMP_Text>(true);
            if (heroLabel != null)
            {
                heroLabel.text = "HERO";
            }

            StyleButton(playBtn, Hex(FableAccent), Hex(FableBg), 32, true);
            StyleButton(heroesBtn, Hex(FablePanel), Hex(FableText), 32, true);
            heroesBtn.GetComponent<Button>().interactable = true;

            string[] soonNames = { "ShopButton", "EventButton", "SettingsButton" };
            string[] soonLabels = { "SHOP", "EVENT", "SETTING" };
            for (int i = 0; i < soonNames.Length; i++)
            {
                var soon = FindOrCloneButton(row.transform, playBtn, soonNames[i], soonLabels[i]);
                soon.transform.SetSiblingIndex(2 + i);
                SizeRowButton(soon);
                StyleButton(soon, new Color(1f, 1f, 1f, 0.06f), Hex(FableMuted), 32, true);
                soon.GetComponent<Button>().interactable = false;
                EnsureSoonBadge(soon);
            }
        }

        private static void MoveToRow(Transform row, GameObject btn, int index)
        {
            if (btn == null)
            {
                return;
            }

            btn.transform.SetParent(row, false);
            btn.transform.SetSiblingIndex(index);
            btn.SetActive(true);
            SizeRowButton(btn);
        }

        private static void SizeRowButton(GameObject btn)
        {
            var layout = btn.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = btn.AddComponent<LayoutElement>();
            }

            // Touch-friendly 170px (di atas minimum 64-96px).
            layout.minWidth = 170f;
            layout.preferredWidth = 170f;
            layout.minHeight = 170f;
            layout.preferredHeight = 170f;

            // Pivot tengah wajib: pivot.x=1 menggeser tombol +85px (setengah lebar)
            // dari slot HorizontalLayoutGroup (kasus PlayButton kemarin).
            var rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        private static void EnsureSoonBadge(GameObject btn)
        {
            var t = btn.transform.Find("SoonBadge");
            GameObject go = t != null ? t.gameObject : new GameObject("SoonBadge", typeof(RectTransform));
            if (t == null)
            {
                go.transform.SetParent(btn.transform, false);
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 16f);
            rt.sizeDelta = new Vector2(150f, 30f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
            }

            tmp.text = string.Empty; // Badge SOON nonaktif (keputusan user): objek dipertahankan, teks dikosongkan.
            tmp.fontSize = 20;
            tmp.color = Hex(FableGold);
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        /// <summary>
        /// Background navy full-stretch + judul WPG_3 + subtitle (gaya screenshot).
        /// </summary>
        private static void StyleMainMenuChrome(Transform mainMenu)
        {
            var bgT = mainMenu.Find("Image");
            if (bgT != null)
            {
                var bg = bgT.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = Hex(FableBg);
                }

                var bgRt = bgT.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                bgT.SetAsFirstSibling();
            }

            var titleT = mainMenu.Find("Title");
            if (titleT != null)
            {
                var tmp = titleT.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = "WPG_3";
                    tmp.fontSize = 110;
                    tmp.color = Hex(FableText);
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.raycastTarget = false;
                }

                var rt = titleT.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -70f);
                rt.sizeDelta = new Vector2(1200f, 160f);
            }

            var subT = mainMenu.Find("Subtitle");
            if (subT != null)
            {
                var tmp = subT.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = "Top-Down Roguelike Prototype";
                    tmp.fontSize = 34;
                    tmp.color = Hex(FableMuted);
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.raycastTarget = false;
                }

                var rt = subT.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -240f);
                rt.sizeDelta = new Vector2(1200f, 60f);
            }
        }

        private static void StyleButton(GameObject go, Color bg, Color labelColor, int labelSize, bool bold)
        {
            if (go == null)
            {
                return;
            }

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.color = bg;
            }

            var tmp = go.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.color = labelColor;
                if (labelSize > 0)
                {
                    tmp.fontSize = labelSize;
                }

                tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        [MenuItem("Tools/WPG_3/Fix Center Layout")]
        public static void FixCenterLayout()
        {
            var ui = GameObject.Find("MenuUI");
            var centerT = ui != null ? ui.transform.Find("ShowcasePanel/Center") : null;
            if (centerT == null)
            {
                Debug.LogError("[CharSelect] Center tidak ditemukan.");
                return;
            }

            var center = centerT.gameObject;
            EnforceCenterLayout(center);

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[CharSelect] Center layout diperbaiki.");
        }

        private static void EnforceCenterLayout(GameObject center)
        {
            var layout = center.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = center.AddComponent<VerticalLayoutGroup>();
            }

            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            // Tinggi dikunci (min == preferred, lihat LockHeight) sehingga parent
            // menumpuk deterministik — pola yang sama dengan baris detail.
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 10, 10);

            var fitter = center.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = center.AddComponent<ContentSizeFitter>();
            }

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void EnsureMinHeight(GameObject go, float minHeight)
        {
            if (go == null)
            {
                return;
            }

            var layout = go.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = go.AddComponent<LayoutElement>();
            }

            layout.minHeight = Mathf.Max(layout.minHeight, minHeight);
        }

        /// <summary>
        /// Kunci tinggi elemen (min + preferred sama) untuk parent yang mengontrol
        /// tinggi child — penumpukan deterministik tanpa tergantung preferred TMP.
        /// </summary>
        private static void LockHeight(GameObject go, float height)
        {
            if (go == null)
            {
                return;
            }

            var layout = go.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = go.AddComponent<LayoutElement>();
            }

            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        /// <summary>
        /// Pastikan prefab template baris ada di Project (buat bila hilang).
        /// ADDITIVE: struktur yang hilang dilengkapi, nilai styling yang sudah ada
        /// TIDAK PERNAH ditimpa — jadi aman di-run ulang setelah user mengedit
        /// prefab secara visual. Kembalikan aset prefab untuk di-wire ke controller.
        /// </summary>
        private static GameObject EnsureRowPrefab(string path, string rootName, float height, bool paragraph)
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");

            GameObject temp;
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                temp = (GameObject)PrefabUtility.InstantiatePrefab(existing);
            }
            else
            {
                temp = new GameObject(rootName, typeof(RectTransform));
            }

            temp.name = rootName;

            var layout = temp.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = temp.AddComponent<LayoutElement>();
            }

            if (layout.minHeight <= 0f && layout.preferredHeight <= 0f)
            {
                layout.minHeight = height;
                layout.preferredHeight = height;
            }

            if (paragraph)
            {
                EnsureRowText(temp.transform, "Title", Hex(FableAccent), 24, FontStyles.Bold,
                    TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    Vector2.zero, new Vector2(0f, 52f));
                EnsureRowText(temp.transform, "Body", Hex(FableText), 24, FontStyles.Normal,
                    TextAlignmentOptions.TopLeft,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(0f, -56f));
            }
            else
            {
                EnsureRowText(temp.transform, "Title", Hex(FableMuted), 24, FontStyles.Normal,
                    TextAlignmentOptions.Left,
                    Vector2.zero, new Vector2(0.55f, 1f), new Vector2(0f, 0.5f),
                    Vector2.zero, Vector2.zero);
                EnsureRowText(temp.transform, "Value", Hex(FableText), 24, FontStyles.Bold,
                    TextAlignmentOptions.Right,
                    new Vector2(0.55f, 0f), Vector2.one, new Vector2(1f, 0.5f),
                    Vector2.zero, Vector2.zero);
            }

            var saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
            UnityEngine.Object.DestroyImmediate(temp);
            if (saved == null)
            {
                Debug.LogError("[CharSelect] Gagal menyimpan prefab: " + path);
            }

            return saved;
        }

        /// <summary>
        /// Pastikan anak TMP ada di root prefab. Hanya TMP yang BARU dibuat yang
        /// diberi style + rect; yang sudah ada dibiarkan (styling milik user).
        /// </summary>
        private static void EnsureRowText(Transform parent, string name, Color color, int size,
            FontStyles style, TextAlignmentOptions align,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var t = parent.Find(name);
            GameObject go = t != null ? t.gameObject : new GameObject(name, typeof(RectTransform));
            if (t == null)
            {
                go.transform.SetParent(parent, false);
            }

            var rt = go.GetComponent<RectTransform>();
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                return;
            }

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            tmp.color = color;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            tmp.margin = new Vector4(12f, 4f, 12f, 4f);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            string parent = slash > 0 ? path.Substring(0, slash) : "Assets";
            string name = slash > 0 ? path.Substring(slash + 1) : path;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        [MenuItem("Tools/WPG_3/Validate Slice")]
        public static void ValidateSlice()
        {
            var controller = Object.FindFirstObjectByType<CharacterSelectController>();
            if (controller == null)
            {
                Debug.LogError("[SliceCheck] FAIL: CharacterSelectController tidak ditemukan.");
                return;
            }

            controller.ShowSelect();
            int cards = CountActiveCards(controller);
            controller.SetSelected(0);
            int statRows = CountDetailRows(controller);
            controller.OnTab(1);
            int skillRows = CountDetailRows(controller);
            controller.OnTab(2);
            int pasifRows = CountDetailRows(controller);
            controller.OnTab(0);

            bool ok = cards == 4 && statRows == 20 && skillRows == 3 && pasifRows == 1;
            controller.CleanupRuntime();
            Debug.Log("[SliceCheck] MENU cards=" + cards + " statRows=" + statRows
                + " skillRows=" + skillRows + " pasifRows=" + pasifRows
                + " => " + (ok ? "PASS" : "FAIL"));
        }

        [MenuItem("Tools/WPG_3/Validate Slice Tab")]
        public static void ValidateSliceTab()
        {
            var controller = Object.FindFirstObjectByType<CharacterSelectController>();
            if (controller == null)
            {
                Debug.LogError("[SliceCheck] FAIL: controller hilang.");
                return;
            }

            var so = new SerializedObject(controller);
            int tab = so.FindProperty("activeTab").intValue;
            int rows = CountDetailRows(controller);
            Debug.Log("[SliceCheck] TAB tab=" + tab + " rows=" + rows);
        }

        [MenuItem("Tools/WPG_3/Validate Slice Go")]
        public static void ValidateSliceGo()
        {
            var controller = Object.FindFirstObjectByType<CharacterSelectController>();
            if (controller == null)
            {
                Debug.LogError("[SliceCheck] FAIL: controller hilang (bukan di Main Menu?).");
                return;
            }

            controller.SetSelected(0);
            controller.SelectAndPlay();
            Debug.Log("[SliceCheck] GO: SelectAndPlay dipanggil.");
        }

        [MenuItem("Tools/WPG_3/Validate Slice Main")]
        public static void ValidateSliceMain()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[SliceCheck] FAIL: Player tidak ada di Main.");
                return;
            }

            var stats = player.GetComponent<WpgGame.Player.PlayerStats>();
            var health = player.GetComponent<WpgGame.Combat.HealthSystem>();
            var skill = player.GetComponent<WpgGame.Player.HeroSkill>();
            if (stats == null || health == null || skill == null)
            {
                Debug.LogError("[SliceCheck] FAIL: komponen Player/Health/HeroSkill tidak lengkap.");
                return;
            }

            float before = stats.Damage;
            skill.TryCast(0);
            float buffed = stats.Damage;
            bool ok = Mathf.Approximately(health.MaxHP, 1200f)
                && buffed > before
                && PlayerPrefs.GetString("wpg3_hero") == "aelindra";
            Debug.Log("[SliceCheck] MAIN hp=" + health.MaxHP + " dmg " + before + "->" + buffed
                + " prefs=" + PlayerPrefs.GetString("wpg3_hero")
                + " => " + (ok ? "PASS" : "FAIL"));
        }

        private static int CountActiveCards(CharacterSelectController controller)
        {
            int n = 0;
            var so = new SerializedObject(controller);
            var grid = so.FindProperty("cardGrid").objectReferenceValue as Transform;
            if (grid == null)
            {
                return -1;
            }

            foreach (Transform child in grid)
            {
                if (child.gameObject.activeSelf)
                {
                    n++;
                }
            }

            return n;
        }

        private static int CountDetailRows(CharacterSelectController controller)
        {
            var so = new SerializedObject(controller);
            var content = so.FindProperty("detailContent").objectReferenceValue as Transform;
            if (content == null)
            {
                return -1;
            }

            return content.childCount;
        }

        private static void SetObj(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p == null)
            {
                Debug.LogError("[CharSelect] Property tidak ada: " + prop);
                return;
            }

            p.objectReferenceValue = value;
        }

        private static void SetObj(SerializedObject so, string prop, Object[] values)
        {
            var p = so.FindProperty(prop);
            if (p == null)
            {
                Debug.LogError("[CharSelect] Property tidak ada: " + prop);
                return;
            }

            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static GameObject FindOrCreate(Transform parent, string name, params System.Type[] components)
        {
            var t = parent.Find(name);
            if (t != null)
            {
                return t.gameObject;
            }

            var go = new GameObject(name, components);
            /* Undo skipped: go, "Build Character Select" */
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject FindOrCreatePanel(Transform parent, string name, Color color)
        {
            var go = FindOrCreate(parent, name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static GameObject FindOrCloneButton(Transform parent, GameObject source, string name, string label)
        {
            var t = parent.Find(name);
            GameObject go;
            if (t != null)
            {
                go = t.gameObject;
            }
            else
            {
                go = Object.Instantiate(source, parent, false);
                /* Undo skipped: go, "Build Character Select" */
                go.name = name;
            }

            var tmp = go.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = label;
            }

            go.SetActive(true);
            return go;
        }

        private static void ShiftBelow(RectTransform rt, RectTransform above, float gap)
        {
            rt.anchorMin = above.anchorMin;
            rt.anchorMax = above.anchorMax;
            rt.pivot = above.pivot;
            rt.sizeDelta = above.sizeDelta;
            rt.anchoredPosition = above.anchoredPosition + new Vector2(0f, -(above.sizeDelta.y + gap));
        }

        private static TMP_Text AddText(Transform parent, string name, string text, int size, Color color)
        {
            var t = parent.Find(name);
            GameObject go = t != null ? t.gameObject : new GameObject(name, typeof(RectTransform));
            if (t == null)
            {
                /* Undo skipped: go, "Build Character Select" */
                go.transform.SetParent(parent, false);
            }

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
            }

            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void AddTitle(Transform parent, string name, string text, Color color, int size)
        {
            var tmp = AddText(parent, name, text, size, color);
            tmp.fontStyle = FontStyles.Bold;
            var rt = tmp.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -30f);
            rt.sizeDelta = new Vector2(1200f, 90f);
        }

        private static void AnchorStretchCenter(RectTransform rt, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }

        private static void AnchorBottom(RectTransform rt, Vector2 offset)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = offset;
        }

        private static void AnchorLeft(RectTransform rt, float width)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(width, 700f);
            rt.anchoredPosition = new Vector2(60f, 0f);
        }

        private static void AnchorCenter(RectTransform rt, Vector2 size, Vector2 offset)
        {
            AnchorStretchCenter(rt, size, offset);
        }

        private static void AnchorRight(RectTransform rt, float width)
        {
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(width, 700f);
            rt.anchoredPosition = new Vector2(-60f, 0f);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null)
            {
                c = go.AddComponent<T>();
            }

            return c;
        }

        private static GameObject BuildCardTemplate(Transform grid, Button styleSource, TMP_Text styleLabel)
        {
            var t = grid.Find("CardTemplate");
            GameObject go = t != null ? t.gameObject : Object.Instantiate(styleSource.gameObject, grid, false);
            if (t == null)
            {
                /* Undo skipped: go, "Build Character Select" */
            }

            go.name = "CardTemplate";
            var btn = go.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280f, 620f);

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.color = Hex(FablePanel);
            }

            // Strip aksen cyan di tepi atas kartu (ala ::before fable.html).
            var strip = FindOrCreate(go.transform, "AccentStrip",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var stripRt = strip.GetComponent<RectTransform>();
            stripRt.anchorMin = new Vector2(0f, 1f);
            stripRt.anchorMax = new Vector2(1f, 1f);
            stripRt.pivot = new Vector2(0.5f, 1f);
            stripRt.anchoredPosition = Vector2.zero;
            stripRt.sizeDelta = new Vector2(0f, 10f);
            var stripImg = strip.GetComponent<Image>();
            stripImg.color = Hex(FableAccent);
            stripImg.raycastTarget = false;

            var oldLabel = go.GetComponentInChildren<TMP_Text>(true);
            TMP_Text portrait = AddText(go.transform, "PortraitText", "AE", 120, Hex(FableAccent));
            portrait.fontStyle = FontStyles.Bold;
            var prt = portrait.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 1f);
            prt.anchorMax = new Vector2(0.5f, 1f);
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -140f);
            prt.sizeDelta = new Vector2(240f, 160f);

            TMP_Text nameText = AddText(go.transform, "NameText", "Hero", 40, Hex(FableText));
            nameText.fontStyle = FontStyles.Bold;
            var nrt = nameText.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.5f, 0f);
            nrt.anchorMax = new Vector2(0.5f, 0f);
            nrt.pivot = new Vector2(0.5f, 0f);
            nrt.anchoredPosition = new Vector2(0f, 150f);
            nrt.sizeDelta = new Vector2(240f, 60f);

            TMP_Text roleText = AddText(go.transform, "RoleText", "Role", 30,
                Hex(FableMuted));
            var rrt = roleText.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.5f, 0f);
            rrt.anchorMax = new Vector2(0.5f, 0f);
            rrt.pivot = new Vector2(0.5f, 0f);
            rrt.anchoredPosition = new Vector2(0f, 100f);
            rrt.sizeDelta = new Vector2(240f, 50f);

            TMP_Text badge = AddText(go.transform, "BadgeText", "LOCKED", 28, Hex(FableDanger));
            badge.fontStyle = FontStyles.Bold;
            var brt = badge.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0f);
            brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0f, 50f);
            brt.sizeDelta = new Vector2(240f, 44f);

            var hl = go.transform.Find("SelectedHighlight");
            GameObject hlGO = hl != null ? hl.gameObject
                : new GameObject("SelectedHighlight", typeof(RectTransform), typeof(Image));
            if (hl == null)
            {
                /* Undo skipped: hlGO, "Build Character Select" */
                hlGO.transform.SetParent(go.transform, false);
            }

            var hlRt = hlGO.GetComponent<RectTransform>();
            hlRt.anchorMin = Vector2.zero;
            hlRt.anchorMax = Vector2.one;
            hlRt.offsetMin = Vector2.zero;
            hlRt.offsetMax = Vector2.zero;
            var hlImg = hlGO.GetComponent<Image>();
            hlImg.color = new Color(0.22f, 0.74f, 0.97f, 0.35f);
            hlImg.raycastTarget = false;
            hlGO.SetActive(false);

            // Overlay gelap untuk kartu locked (ala .locked-overlay fable.html).
            // Di bawah BadgeText + SelectedHighlight agar keduanya tetap terlihat.
            var overlay = FindOrCreate(go.transform, "LockedOverlay",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var ovRt = overlay.GetComponent<RectTransform>();
            ovRt.anchorMin = Vector2.zero;
            ovRt.anchorMax = Vector2.one;
            ovRt.offsetMin = Vector2.zero;
            ovRt.offsetMax = Vector2.zero;
            var ovImg = overlay.GetComponent<Image>();
            ovImg.color = new Color(0.059f, 0.09f, 0.165f, 0.72f);
            ovImg.raycastTarget = false;
            badge.transform.SetAsLastSibling();
            hlGO.transform.SetAsLastSibling();

            if (oldLabel != null && oldLabel.gameObject != portrait.gameObject
                && oldLabel.gameObject != nameText.gameObject)
            {
                oldLabel.gameObject.SetActive(false);
            }

            var card = GetOrAdd<CharacterCard>(go);
            var cardSo = new SerializedObject(card);
            cardSo.FindProperty("cardButton").objectReferenceValue = btn;
            cardSo.FindProperty("nameText").objectReferenceValue = nameText;
            cardSo.FindProperty("roleText").objectReferenceValue = roleText;
            cardSo.FindProperty("portraitImage").objectReferenceValue = null;
            cardSo.FindProperty("portraitFallbackText").objectReferenceValue = portrait;
            cardSo.FindProperty("badgeText").objectReferenceValue = badge;
            cardSo.FindProperty("selectedHighlight").objectReferenceValue = hlGO;
            cardSo.FindProperty("lockedOverlay").objectReferenceValue = overlay;
            cardSo.FindProperty("accentStrip").objectReferenceValue = stripImg;
            cardSo.ApplyModifiedPropertiesWithoutUndo();

            go.SetActive(false);
            return go;
        }
    }
}
#endif
