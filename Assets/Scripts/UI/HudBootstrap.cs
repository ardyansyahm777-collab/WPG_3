#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WpgGame.UI
{
    /// <summary>
    /// Tooling sekali-pakai (lead): melengkapi hierarki HUD Main mengikuti maket
    /// Workstream A — ExpBar (existing) / Portrait / HP (existing) / Shield /
    /// Timer / Gold (existing) / Kill / SkillBar (2 ability, tanpa pasif) — plus
    /// StatsText victory di GameOverScreen. Menu: Tools/WPG_3/Build HUD Main.
    /// Idempoten: objek yang sudah ada dipakai ulang, styling existing TIDAK ditimpa
    /// (hanya objek BARU yang diberi default). Flag layout eksplisit tiap Build.
    /// JANGAN hand-edit YAML — jalankan menu ini di Editor scene Main.
    /// </summary>
    public static class HudBootstrap
    {
        private const string SceneName = "Main";

        [MenuItem("Tools/WPG_3/Build HUD Main")]
        public static void Build()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != SceneName)
            {
                Debug.LogError("[HUD] Scene aktif bukan 'Main': " + scene.name);
                return;
            }

            var uiRoot = GameObject.Find("UI");
            if (uiRoot == null)
            {
                Debug.LogError("[HUD] Root 'UI' (Canvas) tidak ditemukan.");
                return;
            }

            var hudT = uiRoot.transform.Find("HUD");
            if (hudT == null)
            {
                Debug.LogError("[HUD] 'UI/HUD' + HUDController tidak ditemukan.");
                return;
            }

            var hud = hudT.GetComponent<HUDController>();
            if (hud == null)
            {
                Debug.LogError("[HUD] HUDController hilang di UI/HUD.");
                return;
            }

            // Template clone (styling milik scene — dipakai ulang, tidak diubah).
            var hpBarGO = uiRoot.transform.Find("HUD/HpBar") != null
                ? uiRoot.transform.Find("HUD/HpBar").gameObject : null;
            var levelTextGO = uiRoot.transform.Find("HUD/LevelText") != null
                ? uiRoot.transform.Find("HUD/LevelText").gameObject : null;
            var goldTextGO = uiRoot.transform.Find("HUD/GoldText") != null
                ? uiRoot.transform.Find("HUD/GoldText").gameObject : null;

            // 1. Portrait kiri-atas (Image hiasan + TMP inisial fallback).
            var portraitGO = FindOrCreate(hudT, "Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var portraitRt = portraitGO.GetComponent<RectTransform>();
            portraitRt.anchorMin = new Vector2(0f, 1f);
            portraitRt.anchorMax = new Vector2(0f, 1f);
            portraitRt.pivot = new Vector2(0f, 1f);
            portraitRt.anchoredPosition = new Vector2(24f, -70f);
            if (portraitRt.sizeDelta.x <= 0f || portraitRt.sizeDelta.y <= 0f)
            {
                portraitRt.sizeDelta = new Vector2(120f, 120f);
            }

            var portraitImg = portraitGO.GetComponent<Image>();
            if (portraitImg != null && portraitImg.color == Color.white)
            {
                portraitImg.color = new Color(0.13f, 0.18f, 0.29f, 1f);
            }

            var portraitTextGO = FindOrCreate(portraitGO.transform, "PortraitText", typeof(RectTransform));
            var portraitTmp = GetOrAdd<TextMeshProUGUI>(portraitTextGO);
            var portraitTextRt = portraitTextGO.GetComponent<RectTransform>();
            portraitTextRt.anchorMin = Vector2.zero;
            portraitTextRt.anchorMax = Vector2.one;
            portraitTextRt.offsetMin = Vector2.zero;
            portraitTextRt.offsetMax = Vector2.zero;
            if (string.IsNullOrEmpty(portraitTmp.text))
            {
                portraitTmp.text = "AE";
            }

            portraitTmp.fontSize = portraitTmp.fontSize > 0 ? portraitTmp.fontSize : 56;
            portraitTmp.alignment = TextAlignmentOptions.Center;
            portraitTmp.fontStyle = FontStyles.Bold;
            portraitTmp.textWrappingMode = TextWrappingModes.NoWrap;
            portraitTmp.overflowMode = TextOverflowModes.Ellipsis;
            portraitTmp.raycastTarget = false;

            // 1b. LevelText eksplisit kiri-atas (di atas portrait, bebas tumpang).
            // ExpBar full-width paling atas; LevelText di (24,-8) size 160x40,
            // portrait di (24,-70) size 120 -> vertikal tidak tumpang (-8..-48 vs -70..-190).
            if (levelTextGO != null)
            {
                var levelRt = levelTextGO.GetComponent<RectTransform>();
                if (levelRt != null)
                {
                    levelRt.anchorMin = new Vector2(0f, 1f);
                    levelRt.anchorMax = new Vector2(0f, 1f);
                    levelRt.pivot = new Vector2(0f, 1f);
                    levelRt.anchoredPosition = new Vector2(24f, -8f);
                    levelRt.sizeDelta = new Vector2(160f, 40f);
                }

                var levelTmp = GetOrAdd<TextMeshProUGUI>(levelTextGO);
                levelTmp.fontSize = 30;
                levelTmp.alignment = TextAlignmentOptions.Left;
                levelTmp.overflowMode = TextOverflowModes.Ellipsis;
                levelTmp.textWrappingMode = TextWrappingModes.NoWrap;
                levelTmp.raycastTarget = false;
            }

            // 2. ShieldBar (clone HpBar agar Slider fill refs valid) + ShieldText angka.
            var shieldBarGO = hudT.Find("ShieldBar") != null ? hudT.Find("ShieldBar").gameObject : null;
            if (shieldBarGO == null && hpBarGO != null)
            {
                shieldBarGO = Object.Instantiate(hpBarGO, hudT, false);
                shieldBarGO.name = "ShieldBar";
            }

            if (shieldBarGO == null)
            {
                shieldBarGO = CreateSliderFresh(hudT, "ShieldBar");
            }

            var shieldRt = shieldBarGO.GetComponent<RectTransform>();
            shieldRt.anchorMin = new Vector2(0f, 1f);
            shieldRt.anchorMax = new Vector2(0f, 1f);
            shieldRt.pivot = new Vector2(0f, 0.5f);
            shieldRt.anchoredPosition = new Vector2(166f, -168f);
            shieldRt.sizeDelta = new Vector2(420f, 24f);

            var shieldSlider = shieldBarGO.GetComponent<Slider>();
            if (shieldSlider != null)
            {
                shieldSlider.minValue = 0f;
                shieldSlider.maxValue = 1f;
                shieldSlider.value = 0f;
            }

            var shieldTextGO = hudT.Find("ShieldText") != null ? hudT.Find("ShieldText").gameObject : null;
            if (shieldTextGO == null)
            {
                shieldTextGO = goldTextGO != null
                    ? Object.Instantiate(goldTextGO, hudT, false)
                    : new GameObject("ShieldText", typeof(RectTransform));
                if (shieldTextGO.transform.parent != hudT)
                {
                    shieldTextGO.transform.SetParent(hudT, false);
                }

                shieldTextGO.name = "ShieldText";
            }

            var shieldTextRt = shieldTextGO.GetComponent<RectTransform>();
            shieldTextRt.anchorMin = new Vector2(0f, 1f);
            shieldTextRt.anchorMax = new Vector2(0f, 1f);
            shieldTextRt.pivot = new Vector2(0f, 0.5f);
            shieldTextRt.anchoredPosition = new Vector2(596f, -168f);
            shieldTextRt.sizeDelta = new Vector2(120f, 36f);
            var shieldTmp = GetOrAdd<TextMeshProUGUI>(shieldTextGO);
            if (string.IsNullOrEmpty(shieldTmp.text))
            {
                shieldTmp.text = "0";
            }

            shieldTmp.fontSize = shieldTmp.fontSize > 0 ? shieldTmp.fontSize : 26;
            shieldTmp.alignment = TextAlignmentOptions.Left;
            shieldTmp.raycastTarget = false;

            // 3. Timer tengah-atas (M:SS).
            var timerGO = hudT.Find("TimerText") != null ? hudT.Find("TimerText").gameObject : null;
            if (timerGO == null)
            {
                timerGO = levelTextGO != null
                    ? Object.Instantiate(levelTextGO, hudT, false)
                    : new GameObject("TimerText", typeof(RectTransform));
                if (timerGO.transform.parent != hudT)
                {
                    timerGO.transform.SetParent(hudT, false);
                }

                timerGO.name = "TimerText";
            }

            var timerRt = timerGO.GetComponent<RectTransform>();
            timerRt.anchorMin = new Vector2(0.5f, 1f);
            timerRt.anchorMax = new Vector2(0.5f, 1f);
            timerRt.pivot = new Vector2(0.5f, 1f);
            timerRt.anchoredPosition = new Vector2(0f, -70f);
            timerRt.sizeDelta = new Vector2(300f, 64f);
            var timerTmp = GetOrAdd<TextMeshProUGUI>(timerGO);
            if (string.IsNullOrEmpty(timerTmp.text) || timerTmp.text.StartsWith("Lv"))
            {
                timerTmp.text = "5:00";
            }

            timerTmp.fontSize = timerTmp.fontSize > 0 ? timerTmp.fontSize : 48;
            timerTmp.alignment = TextAlignmentOptions.Center;
            timerTmp.fontStyle = FontStyles.Bold;
            timerTmp.raycastTarget = false;

            // 4. Kill kanan (di bawah GoldText).
            var killGO = hudT.Find("KillText") != null ? hudT.Find("KillText").gameObject : null;
            if (killGO == null)
            {
                killGO = goldTextGO != null
                    ? Object.Instantiate(goldTextGO, hudT, false)
                    : new GameObject("KillText", typeof(RectTransform));
                if (killGO.transform.parent != hudT)
                {
                    killGO.transform.SetParent(hudT, false);
                }

                killGO.name = "KillText";
            }

            var killRt = killGO.GetComponent<RectTransform>();
            killRt.anchorMin = new Vector2(1f, 1f);
            killRt.anchorMax = new Vector2(1f, 1f);
            killRt.pivot = new Vector2(1f, 1f);
            killRt.anchoredPosition = new Vector2(-91f, -120f);
            killRt.sizeDelta = new Vector2(260f, 44f);
            var killTmp = GetOrAdd<TextMeshProUGUI>(killGO);
            if (string.IsNullOrEmpty(killTmp.text) || killTmp.text == "0")
            {
                killTmp.text = "KILL 0";
            }

            killTmp.alignment = TextAlignmentOptions.Right;
            killTmp.raycastTarget = false;

            // 5. SkillBar bawah-tengah: HLG eksplisit + 2 Button (tanpa pasif).
            var skillBarGO = FindOrCreate(hudT, "SkillBar", typeof(RectTransform));
            var skillBarRt = skillBarGO.GetComponent<RectTransform>();
            skillBarRt.anchorMin = new Vector2(0.5f, 0f);
            skillBarRt.anchorMax = new Vector2(0.5f, 0f);
            skillBarRt.pivot = new Vector2(0.5f, 0f);
            skillBarRt.anchoredPosition = new Vector2(0f, 60f);
            skillBarRt.sizeDelta = new Vector2(360f, 170f);
            var hlg = GetOrAdd<HorizontalLayoutGroup>(skillBarGO);
            hlg.spacing = 20f;
            hlg.padding = new RectOffset(10, 10, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var s1 = EnsureSkillButton(skillBarGO.transform, "Skill1Button", "1");
            var s2 = EnsureSkillButton(skillBarGO.transform, "Skill2Button", "2");
            var passiveT = skillBarGO.transform.Find("PassiveSlot");
            if (passiveT != null)
            {
                passiveT.gameObject.SetActive(false);
            }

            var skillBar = GetOrAdd<SkillBarUI>(skillBarGO);

            // 6. GameOverScreen: StatsText victory (aditif, hidden saat kalah).
            var goScreenGO = GameObject.Find("UI/GameOverScreen");
            GameObject statsGO = null;
            if (goScreenGO != null)
            {
                var screen = goScreenGO.GetComponent<GameOverScreen>();
                var panelT = goScreenGO.transform.Find("Panel");
                if (panelT != null)
                {
                    var titleT = panelT.Find("Title");
                    statsGO = panelT.Find("StatsText") != null ? panelT.Find("StatsText").gameObject : null;
                    if (statsGO == null)
                    {
                        if (titleT != null)
                        {
                            statsGO = Object.Instantiate(titleT.gameObject, panelT, false);
                        }
                        else
                        {
                            statsGO = new GameObject("StatsText", typeof(RectTransform));
                            statsGO.transform.SetParent(panelT, false);
                            statsGO.AddComponent<TextMeshProUGUI>();
                        }

                        statsGO.name = "StatsText";
                    }

                    var statsRt = statsGO.GetComponent<RectTransform>();
                    statsRt.anchorMin = new Vector2(0f, 0.5f);
                    statsRt.anchorMax = new Vector2(1f, 0.5f);
                    statsRt.pivot = new Vector2(0.5f, 0.5f);
                    statsRt.anchoredPosition = new Vector2(0f, -20f);
                    statsRt.sizeDelta = new Vector2(0f, 120f);
                    var statsTmp = statsGO.GetComponent<TextMeshProUGUI>();
                    if (statsTmp != null)
                    {
                        statsTmp.fontSize = 36;
                        statsTmp.alignment = TextAlignmentOptions.Center;
                        statsTmp.raycastTarget = false;
                    }

                    statsGO.SetActive(false);
                }

                if (screen != null && statsGO != null)
                {
                    var so = new SerializedObject(screen);
                    so.FindProperty("statsText").objectReferenceValue = statsGO.GetComponent<TextMeshProUGUI>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(screen);
                }
            }

            // 7. Wiring HUDController + SkillBarUI (existing refs tidak ditimpa bila sudah terisi).
            var hudSo = new SerializedObject(hud);
            SetObjIfEmpty(hudSo, "shieldBar", shieldBarGO.GetComponent<Slider>());
            SetObjIfEmpty(hudSo, "shieldText", shieldTmp);
            SetObjIfEmpty(hudSo, "timerText", timerTmp);
            SetObjIfEmpty(hudSo, "killText", killTmp);
            SetObjIfEmpty(hudSo, "portraitImage", portraitImg);
            SetObjIfEmpty(hudSo, "portraitText", portraitTmp);
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            var barSo = new SerializedObject(skillBar);
            SetObjIfEmpty(barSo, "skill1Button", s1.button);
            SetObjIfEmpty(barSo, "skill2Button", s2.button);
            SetObjIfEmpty(barSo, "skill1CooldownOverlay", s1.overlay);
            SetObjIfEmpty(barSo, "skill2CooldownOverlay", s2.overlay);
            SetObjIfEmpty(barSo, "skill1CooldownText", s1.cdText);
            SetObjIfEmpty(barSo, "skill2CooldownText", s2.cdText);
            SetObjIfEmpty(barSo, "skill1NameText", s1.nameText);
            SetObjIfEmpty(barSo, "skill2NameText", s2.nameText);
            var passiveIconProp = barSo.FindProperty("passiveIcon");
            if (passiveIconProp != null)
            {
                passiveIconProp.objectReferenceValue = null;
            }

            var passiveNameProp = barSo.FindProperty("passiveNameText");
            if (passiveNameProp != null)
            {
                passiveNameProp.objectReferenceValue = null;
            }

            var passiveDescProp = barSo.FindProperty("passiveDescText");
            if (passiveDescProp != null)
            {
                passiveDescProp.objectReferenceValue = null;
            }

            barSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(skillBar);
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[HUD] Build selesai: Portrait/Shield/Timer/Kill/SkillBar + StatsText victory + wiring.");
        }

        private struct SkillButtonRefs
        {
            public Button button;
            public Image overlay;
            public TMP_Text cdText;
            public TMP_Text nameText;
        }

        private static SkillButtonRefs EnsureSkillButton(Transform parent, string name, string hotkey)
        {
            var t = parent.Find(name);
            GameObject go = t != null ? t.gameObject : new GameObject(name, typeof(RectTransform));
            if (t == null)
            {
                go.transform.SetParent(parent, false);
            }

            var rt = go.GetComponent<RectTransform>();
            var le = GetOrAdd<LayoutElement>(go);
            le.minWidth = 150f;
            le.preferredWidth = 150f;
            le.minHeight = 150f;
            le.preferredHeight = 150f;

            var img = GetOrAdd<Image>(go);
            if (img.color == Color.white)
            {
                img.color = new Color(0.13f, 0.18f, 0.29f, 0.95f);
            }

            var btn = GetOrAdd<Button>(go);
            btn.interactable = true;
            if (btn.targetGraphic == null)
            {
                btn.targetGraphic = img;
            }

            if (go.GetComponent<CanvasGroup>() == null)
            {
                go.AddComponent<CanvasGroup>();
            }

            // Nama skill (atas).
            var nameT = go.transform.Find("SkillName") != null ? go.transform.Find("SkillName").gameObject : null;
            if (nameT == null)
            {
                nameT = new GameObject("SkillName", typeof(RectTransform));
                nameT.transform.SetParent(go.transform, false);
            }

            var nameRt = nameT.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -6f);
            nameRt.sizeDelta = new Vector2(0f, 36f);
            var nameTmp = GetOrAdd<TextMeshProUGUI>(nameT);
            if (string.IsNullOrEmpty(nameTmp.text))
            {
                nameTmp.text = hotkey == "1" ? "Skill 1" : "Skill 2";
            }

            nameTmp.fontSize = 22;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.raycastTarget = false;

            // Hotkey hint (tengah, besar).
            var labelT = go.transform.Find("Label") != null ? go.transform.Find("Label").gameObject : null;
            if (labelT == null)
            {
                labelT = new GameObject("Label", typeof(RectTransform));
                labelT.transform.SetParent(go.transform, false);
            }

            var labelRt = labelT.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.5f, 0.5f);
            labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.anchoredPosition = new Vector2(0f, -8f);
            labelRt.sizeDelta = new Vector2(120f, 80f);
            var labelTmp = GetOrAdd<TextMeshProUGUI>(labelT);
            if (string.IsNullOrEmpty(labelTmp.text))
            {
                labelTmp.text = hotkey;
            }

            labelTmp.fontSize = 64;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.raycastTarget = false;

            // Overlay cooldown radial gelap full-stretch.
            var ovT = go.transform.Find("CooldownOverlay") != null ? go.transform.Find("CooldownOverlay").gameObject : null;
            if (ovT == null)
            {
                ovT = new GameObject("CooldownOverlay", typeof(RectTransform));
                ovT.transform.SetParent(go.transform, false);
            }

            var ovRt = ovT.GetComponent<RectTransform>();
            ovRt.anchorMin = Vector2.zero;
            ovRt.anchorMax = Vector2.one;
            ovRt.offsetMin = Vector2.zero;
            ovRt.offsetMax = Vector2.zero;
            var ovImg = GetOrAdd<Image>(ovT);
            ovImg.type = Image.Type.Filled;
            ovImg.fillMethod = Image.FillMethod.Radial360;
            ovImg.fillOrigin = (int)Image.Origin360.Top;
            ovImg.fillClockwise = false;
            ovImg.fillAmount = 0f;
            if (ovImg.color == Color.white)
            {
                ovImg.color = new Color(0f, 0f, 0f, 0.65f);
            }

            ovImg.raycastTarget = false;
            ovT.SetActive(false);

            // Angka cooldown (tengah).
            var cdT = go.transform.Find("CooldownText") != null ? go.transform.Find("CooldownText").gameObject : null;
            if (cdT == null)
            {
                cdT = new GameObject("CooldownText", typeof(RectTransform));
                cdT.transform.SetParent(go.transform, false);
            }

            var cdRt = cdT.GetComponent<RectTransform>();
            cdRt.anchorMin = new Vector2(0.5f, 0.5f);
            cdRt.anchorMax = new Vector2(0.5f, 0.5f);
            cdRt.pivot = new Vector2(0.5f, 0.5f);
            cdRt.anchoredPosition = Vector2.zero;
            cdRt.sizeDelta = new Vector2(120f, 80f);
            var cdTmp = GetOrAdd<TextMeshProUGUI>(cdT);
            cdTmp.fontSize = 48;
            cdTmp.alignment = TextAlignmentOptions.Center;
            cdTmp.fontStyle = FontStyles.Bold;
            cdTmp.raycastTarget = false;
            cdT.SetActive(false);

            go.SetActive(true);
            return new SkillButtonRefs
            {
                button = btn,
                overlay = ovImg,
                cdText = cdTmp,
                nameText = nameTmp
            };
        }

        private static GameObject CreateSliderFresh(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var slider = go.AddComponent<Slider>();
            var bg = FindOrCreate(go.transform, "Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            var fillArea = FindOrCreate(go.transform, "Fill Area", typeof(RectTransform));
            var fillRt = fillArea.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fill = FindOrCreate(fillArea.transform, "Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0.25f, 0.6f, 1f, 1f);
            slider.targetGraphic = bgImg;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            return go;
        }

        private static GameObject FindOrCreate(Transform parent, string name, params System.Type[] components)
        {
            var t = parent.Find(name);
            if (t != null)
            {
                return t.gameObject;
            }

            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
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

        private static void SetObjIfEmpty(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p == null)
            {
                Debug.LogError("[HUD] Property tidak ada: " + prop);
                return;
            }

            if (p.objectReferenceValue == null && value != null)
            {
                p.objectReferenceValue = value;
            }
        }
    }
}
#endif
