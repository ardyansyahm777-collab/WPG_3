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
            var playBtn = GameObject.Find("MenuUI/MainMenu/PlayButton");
            if (menuUI == null || mainMenu == null || playBtn == null)
            {
                Debug.LogError("[CharSelect] Struktur MenuUI/MainMenu/PlayButton tidak ditemukan.");
                return;
            }

            var playButton = playBtn.GetComponent<Button>();
            var playLabel = playBtn.GetComponentInChildren<TMP_Text>(true);

            // 1. HeroesButton (clone PlayButton agar style konsisten).
            var heroesBtn = FindOrCloneButton(mainMenu.transform, playBtn, "HeroesButton", "HEROES");
            ShiftBelow(heroesBtn.GetComponent<RectTransform>(), playBtn.GetComponent<RectTransform>(), 110f);

            // 2. Panels.
            var cardPanel = FindOrCreatePanel(menuUI.transform, "CardSelectPanel",
                new Color(0.96f, 0.95f, 0.92f, 0.97f));
            var showcasePanel = FindOrCreatePanel(menuUI.transform, "ShowcasePanel",
                new Color(0.05f, 0.12f, 0.16f, 0.98f));

            // 3. Card panel isi.
            AddTitle(cardPanel.transform, "CardTitle", "PILIH HERO",
                new Color(0.1f, 0.1f, 0.1f, 1f), 56);
            var cardGrid = FindOrCreate(cardPanel.transform, "CardGrid", typeof(RectTransform));
            var grid = GetOrAdd<GridLayoutGroup>(cardGrid);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(280f, 620f);
            grid.spacing = new Vector2(24f, 24f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(24, 24, 24, 24);
            var gridRt = cardGrid.GetComponent<RectTransform>();
            AnchorStretchCenter(gridRt, new Vector2(1400f, 700f), new Vector2(0f, -30f));
            var cardClose = FindOrCloneButton(cardPanel.transform, playBtn, "CardCloseButton", "TUTUP");
            AnchorBottom(cardClose.GetComponent<RectTransform>(), new Vector2(0f, 90f));

            // Template kartu (inactive, dipakai Instantiate oleh controller).
            var template = BuildCardTemplate(cardGrid.transform, playButton, playLabel);

            // 4. Showcase isi.
            var tabs = FindOrCreate(showcasePanel.transform, "LeftTabs", typeof(RectTransform));
            var tabsLayout = GetOrAdd<VerticalLayoutGroup>(tabs);
            tabsLayout.spacing = 16f;
            tabsLayout.childAlignment = TextAnchor.MiddleCenter;
            AnchorLeft(tabs.GetComponent<RectTransform>(), 200f);
            var tabStat = FindOrCloneButton(tabs.transform, playBtn, "TabStatButton", "STAT");
            var tabSkill = FindOrCloneButton(tabs.transform, playBtn, "TabSkillButton", "SKILL");
            var tabPasif = FindOrCloneButton(tabs.transform, playBtn, "TabPassiveButton", "PASIF");
            foreach (var t in new[] { tabStat, tabSkill, tabPasif })
            {
                var rt = t.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(160f, 96f);
            }

            var center = FindOrCreate(showcasePanel.transform, "Center", typeof(RectTransform));
            AnchorCenter(center.GetComponent<RectTransform>(), new Vector2(520f, 800f), new Vector2(-100f, 0f));
            EnforceCenterLayout(center);
            var portrait = AddText(center.transform, "HeroPortraitText", "AE", 150,
                new Color(0.95f, 0.91f, 0.82f, 1f));
            EnsureMinHeight(portrait.gameObject, 180f);
            var heroName = AddText(center.transform, "HeroNameText", "Aelindra", 64,
                new Color(0.95f, 0.91f, 0.82f, 1f));
            EnsureMinHeight(heroName.gameObject, 95f);
            var heroRole = AddText(center.transform, "HeroRoleText", "Swift Archer", 36,
                new Color(0.75f, 0.75f, 0.75f, 1f));
            EnsureMinHeight(heroRole.gameObject, 60f);
            var selectBtn = FindOrCloneButton(center.transform, playBtn, "SelectPlayButton", "SELECT & PLAY");
            EnsureMinHeight(selectBtn, 95f);
            var gantiBtn = FindOrCloneButton(center.transform, playBtn, "GantiButton", "GANTI HERO");
            EnsureMinHeight(gantiBtn, 95f);

            var scroll = FindOrCreate(showcasePanel.transform, "DetailScroll", typeof(RectTransform), typeof(ScrollRect));
            AnchorRight(scroll.GetComponent<RectTransform>(), 520f);
            var scrollRect = scroll.GetComponent<ScrollRect>();
            var viewport = FindOrCreate(scroll.transform, "Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            var vpImg = viewport.GetComponent<Image>();
            vpImg.color = new Color(0.04f, 0.1f, 0.14f, 1f);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            var content = FindOrCreate(viewport.transform, "DetailContent", typeof(RectTransform));
            var vlg = GetOrAdd<VerticalLayoutGroup>(content);
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(12, 12, 12, 12);
            var fitter = GetOrAdd<ContentSizeFitter>(content);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = vpRt;
            scrollRect.content = content.GetComponent<RectTransform>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            var backBtn = FindOrCloneButton(showcasePanel.transform, playBtn, "BackButton", "KEMBALI");
            AnchorBottom(backBtn.GetComponent<RectTransform>(), new Vector2(0f, 90f));

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
            SetObj(so, "statRowPrefab", (Object)null);
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
            Debug.Log("[CharSelect] Build selesai: HeroesButton + CardSelectPanel + ShowcasePanel + wiring.");
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

            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
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
                img.color = Color.white;
            }

            var oldLabel = go.GetComponentInChildren<TMP_Text>(true);
            TMP_Text portrait = AddText(go.transform, "PortraitText", "AE", 120, Color.black);
            var prt = portrait.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 1f);
            prt.anchorMax = new Vector2(0.5f, 1f);
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -140f);
            prt.sizeDelta = new Vector2(240f, 160f);

            TMP_Text nameText = AddText(go.transform, "NameText", "Hero", 40, Color.black);
            var nrt = nameText.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.5f, 0f);
            nrt.anchorMax = new Vector2(0.5f, 0f);
            nrt.pivot = new Vector2(0.5f, 0f);
            nrt.anchoredPosition = new Vector2(0f, 150f);
            nrt.sizeDelta = new Vector2(240f, 60f);

            TMP_Text roleText = AddText(go.transform, "RoleText", "Role", 30,
                new Color(0.3f, 0.3f, 0.3f, 1f));
            var rrt = roleText.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.5f, 0f);
            rrt.anchorMax = new Vector2(0.5f, 0f);
            rrt.pivot = new Vector2(0.5f, 0f);
            rrt.anchoredPosition = new Vector2(0f, 100f);
            rrt.sizeDelta = new Vector2(240f, 50f);

            TMP_Text badge = AddText(go.transform, "BadgeText", "LOCKED", 28, Color.red);
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
            hlImg.color = new Color(1f, 0.6f, 0.2f, 0.35f);
            hlImg.raycastTarget = false;
            hlGO.SetActive(false);

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
            cardSo.ApplyModifiedPropertiesWithoutUndo();

            go.SetActive(false);
            return go;
        }
    }
}
#endif
