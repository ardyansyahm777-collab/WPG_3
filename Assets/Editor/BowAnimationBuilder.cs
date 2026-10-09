using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace WpgGame.Editor
{
    /// <summary>
    /// Tool: Tools &gt; WPG_3 &gt; Build Player Bow Anims.
    /// 1. Pastikan running_bow.png ter-import sebagai 6 sub-sprite (runbow_0..5).
    /// 2. Rakit 3 clip loop (sprite-swap) + AnimatorController PlayerBow
    ///    (Idle / Move / RunShoot, param Speed float + IsShooting bool)
    ///    ke Assets/Data/Anims/. Rebuild deterministik (aset lama dihapus dulu).
    /// Tanpa dialog agar bisa dipanggil via CLI.
    /// </summary>
    public static class BowAnimationBuilder
    {
        private const string TexturePath = "Assets/Gambar/Karakter/running_bow.png";
        private const string PrefabPath = "Assets/Prefabs/Player.prefab";
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string OutDir = "Assets/Data/Anims/";
        private const string IdleClipPath = "Assets/Data/Anims/PlayerBowIdle.anim";
        private const string MoveClipPath = "Assets/Data/Anims/PlayerBowMove.anim";
        private const string RunShootClipPath = "Assets/Data/Anims/PlayerBowRunShoot.anim";
        private const string ControllerPath = "Assets/Data/Anims/PlayerBow.controller";

        private const string ParamSpeed = "Speed";
        private const string ParamShooting = "IsShooting";
        private const int FrameCount = 6;

        /// <summary>Tinggi karakter target dalam unit dunia (untuk menu Fit Player Visual Scale).</summary>
        private const float TargetCharacterHeight = 1.7f;

        /// <summary>
        /// Tinggi centroid busur (piksel hijau) runbow_0 dari bawah sel, untuk
        /// MuzzleOffset. Ukur ulang bila ganti art (lihat FitPlayerVisualScale).
        /// </summary>
        private const float BowCentroidYpx = 384f;

        [MenuItem("Tools/WPG_3/Build Player Bow Anims")]
        public static void BuildPlayerBow()
        {
            if (!EnsureSliced()) return;
            Sprite[] frames = LoadFrames();
            if (frames.Length != FrameCount)
            {
                string found = frames.Length == 0 ? "(tidak ada)" : string.Join(", ", frames.Select(s => s.name).ToArray());
                Debug.LogError($"[BowBuilder] Butuh {FrameCount} frame runbow_* (grid 6 sel), ketemu {frames.Length}: {found}. " +
                               "Jangan Slice Automatic di Sprite Editor — jalankan ulang menu ini (ia menulis grid deterministik).");
                return;
            }

            Directory.CreateDirectory(OutDir);
            var idle = BuildClip("PlayerBowIdle", new[] { frames[0] }, 6f);
            var move = BuildClip("PlayerBowMove", frames, 6f);
            var run = BuildClip("PlayerBowRunShoot", frames, 12f);
            SaveClip(idle, IdleClipPath);
            SaveClip(move, MoveClipPath);
            SaveClip(run, RunShootClipPath);
            BuildController(idle, move, run);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BowBuilder] OK frames={frames.Length} idle={IdleClipPath} move={MoveClipPath} run={RunShootClipPath} controller={ControllerPath}");
        }

        [MenuItem("Tools/WPG_3/Wire Player Prefab For Bow")]
        public static void WirePlayerPrefabForBow()
        {
            Sprite[] frames = LoadFrames();
            if (frames.Length != FrameCount)
            {
                Debug.LogError("[BowBuilder] runbow_* belum siap. Jalankan Build Player Bow Anims dulu.");
                return;
            }
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("[BowBuilder] PlayerBow.controller belum ada. Jalankan Build Player Bow Anims dulu.");
                return;
            }

            // Tutup scene yang terbuka: instance prefab yang live menahan
            // AssetDatabase sehingga SaveAsPrefabAsset jadi no-op diam-diam.
            OpenEmptySceneForPrefabEdit();

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Wire(root, frames[0], controller);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BowBuilder] Player.prefab wired: Square=PlayerBow.controller+runbow_0, root visual dibersihkan, PlayerAnimator + stat canon.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Visual player HARUS di satu GameObject saja: clip bind path="" mengikat
        /// SpriteRenderer pada GameObject yang sama dengan Animator. Prefab lama punya
        /// SpriteRenderer+Animator ganda (root &amp; child "Square") —Itu causes
        /// dua animator berebut sprite. Bersihkan root, satu-satunya visual di Square.
        /// </summary>
        private static void Wire(GameObject root, Sprite firstFrame, RuntimeAnimatorController controller)
        {
            // Samakan stat prefab dengan canon bootstrap (GameplaySetup.CreatePlayer).
            var stats = root.GetComponent<WpgGame.Player.PlayerStats>();
            if (stats != null)
            {
                stats.MoveSpeed = 5f;
                stats.FireRate = 1.1f;
                stats.Damage = 85f;
            }
            var health = root.GetComponent<WpgGame.Combat.HealthSystem>();
            if (health != null) health.MaxHP = 3f;

            if (root.GetComponent<WpgGame.Player.PlayerAnimator>() == null)
                root.AddComponent<WpgGame.Player.PlayerAnimator>();

            // Buang visual/animator di root — biar Square jadi satu-satunya sumber sprite.
            foreach (var a in root.GetComponents<Animator>()) UnityEngine.Object.DestroyImmediate(a);
            foreach (var s in root.GetComponents<SpriteRenderer>()) UnityEngine.Object.DestroyImmediate(s);

            var visual = root.transform.Find("Square");
            if (visual == null)
            {
                Debug.LogError("[BowBuilder] Child 'Square' tidak ada di prefab.");
                return;
            }

            // Urut: controller dulu, baru sprite. Assign controller bisa menyampling
            // frame default state, sehingga sprite di-set terakhir agar tidak tertimpa.
            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = controller;

            var sr = visual.GetComponent<SpriteRenderer>();
            if (sr == null) sr = visual.gameObject.AddComponent<SpriteRenderer>();
            sr.enabled = true;
            sr.sprite = firstFrame;

            // Muzzle di tinggi busur (bukan di kaki/root): centroid hijau runbow_0
            // diukur 384px dari bawah sel. x=0 agar simetris saat flipX.
            float vscale = visual.localScale.x;
            float muzzleY = visual.localPosition.y + (BowCentroidYpx / 100f) * vscale;
            var shooter = root.GetComponent<WpgGame.Player.AutoAimShooter>();
            if (shooter != null) shooter.MuzzleOffset = new Vector2(0f, muzzleY);
        }

        /// <summary>
        /// Upsert, bukan "skip bila ada": Build Player Bow Anims men-delete &amp;
        /// recreate PlayerBow.controller, sehingga reference di instance scene jadi
        /// null. Menu ini harus repairing instance yang sudah ada, bukan menolaknya.
        /// </summary>
        [MenuItem("Tools/WPG_3/Place Player In Main Scene")]
        public static void PlacePlayerInMainScene()
        {
            Sprite[] frames = LoadFrames();
            if (frames.Length != FrameCount)
            {
                Debug.LogError("[BowBuilder] runbow_* belum siap. Jalankan Build Player Bow Anims dulu.");
                return;
            }
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("[BowBuilder] PlayerBow.controller belum ada. Jalankan Build Player Bow Anims dulu.");
                return;
            }

            if (Application.isPlaying)
            {
                Debug.LogError("[BowBuilder] Stop Play mode dulu — menu ini menulis asset/scene, tidak boleh saat play.");
                return;
            }
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(MainScenePath);
            var existing = Object.FindAnyObjectByType<WpgGame.Player.PlayerController>();

            GameObject instance;
            if (existing != null)
            {
                instance = existing.gameObject;
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null)
                {
                    Debug.LogError("[BowBuilder] Player.prefab tidak ketemu.");
                    return;
                }
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.position = Vector3.zero;
            }

            // Refresh visual pada instance: controller bisa null setelah rebuild.
            var visual = instance.transform.Find("Square");
            if (visual == null)
            {
                Debug.LogError("[BowBuilder] Child 'Square' tidak ada pada instance Player.");
                return;
            }
            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            var sr = visual.GetComponent<SpriteRenderer>();
            if (sr == null) sr = visual.gameObject.AddComponent<SpriteRenderer>();
            sr.enabled = true;
            sr.sprite = frames[0];
            if (instance.GetComponent<WpgGame.Player.PlayerAnimator>() == null)
                instance.AddComponent<WpgGame.Player.PlayerAnimator>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log(existing != null
                ? "[BowBuilder] Instance Player di Main.unity di-repair (controller + sprite + PlayerAnimator) + scene disimpan."
                : "[BowBuilder] Player prefab instance ditaruh di Main.unity (0,0,0) + scene disimpan.");
        }

        private static bool EnsureSliced()
        {
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[BowBuilder] Texture tidak ketemu: {TexturePath}.");
                return false;
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (tex == null)
            {
                Debug.LogError($"[BowBuilder] Gagal load texture: {TexturePath}.");
                return false;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Bilinear;
            // spritePixelsToUnits OBSOLETE di Unity 6000.6 (hanya warning, diam-diam
            // tidak diterapkan) -> PPU harus lewat spritePixelsPerUnit.
            importer.spritePixelsPerUnit = 100f;
            // Strip 7680px: maxTextureSize 2048 mengecilkan texture jadi 2048x213 dan
            // karakter jadi nyaris tak terlihat. 8192 = full res, tetap hemat (hanya
            // 1 texture, kompresi runtime).
            importer.maxTextureSize = 8192;

            // Grid 6 sel horizontal, pitch 1280 (sama dengan idle.png). Slice
            // "Automatic" di Sprite Editor memotong frame (cuddle) — grid eksak
            // di bawah yang dipakai, pivot bottom-center agar kaki napak.
            float cw = tex.width / (float)FrameCount;
            var sheet = new SpriteMetaData[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                sheet[i] = new SpriteMetaData
                {
                    name = $"runbow_{i}",
                    rect = new Rect(i * cw, 0f, cw, tex.height),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f),
                    border = Vector4.zero
                };
            }
            importer.spritesheet = sheet;

            // SaveAndReimport (bukan AssetDatabase.ImportAsset): hanya ini yang
            // menulis importer ke disk. ImportAsset hanya membaca ulang, sehingga
            // spritesheet baru hilang dan slice lama di Inspector menimpa balik.
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            return true;
        }

        /// <summary>
        /// Tools &gt; WPG_3 &gt; Fit Player Visual Scale.
        /// Mengukur piksel opaque runbow_* lalu set scale+offset child "Square" supaya
        /// karakter setinggi <see cref="TargetCharacterHeight"/> unit dunia dan kakinya
        /// rata y=0. Dijalankan tiap ganti art — JANGAN menebak scale manual: sel
        /// 1280x800 punya banyak padding transparan, sehingga scale yang "kelihatan"
        /// di Inspector jauh lebih besar dari yang needed.
        /// </summary>
        [MenuItem("Tools/WPG_3/Fit Player Visual Scale")]
        public static void FitPlayerVisualScale()
        {
            Sprite[] frames = LoadFrames();
            if (frames.Length != FrameCount)
            {
                Debug.LogError("[BowBuilder] runbow_* belum siap. Jalankan Build Player Bow Anims dulu.");
                return;
            }

            // Perlu GetPixels -> aktifkan Read/Write sementara.
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            bool wasReadable = importer.isReadable;
            if (!wasReadable) { importer.isReadable = true; importer.SaveAndReimport(); }

            try
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                if (tex == null)
                {
                    Debug.LogError("[BowBuilder] Gagal load texture.");
                    return;
                }
                float ppu = frames[0].pixelsPerUnit;
                int maxH = 0, minYAll = int.MaxValue;
                foreach (var spr in frames)
                {
                    int w = (int)spr.rect.width, h = (int)spr.rect.height;
                    var px = tex.GetPixels((int)spr.rect.x, (int)spr.rect.y, w, h);
                    int minY = int.MaxValue, maxY = -1;
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            if (px[y * w + x].a <= 0.05f) continue;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                    if (maxY < 0) continue;
                    int ch = maxY - minY;
                    if (ch > maxH) maxH = ch;
                    if (minY < minYAll) minYAll = minY;
                }
                if (maxH == 0)
                {
                    Debug.LogError("[BowBuilder] Semua frame kosong (tidak ada piksel opaque).");
                    return;
                }

                float scale = TargetCharacterHeight / (maxH / ppu);
                // Tanda NEGATIF: kaki (minY px di atas pivot sel) harus jatuh di
                // origin root (y=0), jadi child digeser TURUN sejauh tinggi kaki.
                // Positif = sprite melayang 2x lipat di atas collider.
                float localY = -(minYAll / ppu) * scale;

                OpenEmptySceneForPrefabEdit();

                var root = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    var visual = root.transform.Find("Square");
                    if (visual == null)
                    {
                        Debug.LogError("[BowBuilder] Child 'Square' tidak ada.");
                        return;
                    }
                    visual.localScale = new Vector3(scale, scale, 1f);
                    visual.localPosition = new Vector3(0f, localY, 0f);
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                AssetDatabase.SaveAssets();
                Debug.Log($"[BowBuilder] Visual fitted: charH={maxH}px scale={scale:0.0000} localY={localY:0.0000} (target {TargetCharacterHeight} unit).");
            }
            finally
            {
                if (!wasReadable)
                {
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
            }
        }

        /// <summary>
        /// Tutup scene yang terbuka sebelum menulis asset prefab: instance prefab yang
        /// live menahan AssetDatabase. Save scene dirty dulu supaya tidak ada kerja
        /// yang hilang, lalu NewScene(Empty) — CloseScene tidak didukung untuk scene
        /// terakhir ("Unloading the last loaded scene is not supported").
        /// </summary>
        private static void OpenEmptySceneForPrefabEdit()
        {
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid()) return;
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return; // user cancel — jangan lanjut, scene masih dirty
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);
        }

        private static Sprite[] LoadFrames()
        {
            return AssetDatabase.LoadAllAssetsAtPath(TexturePath)
                .OfType<Sprite>()
                .Where(s => s.name.StartsWith("runbow_"))
                .OrderBy(s => s.name)
                .ToArray();
        }

        private static AnimationClip BuildClip(string name, Sprite[] frames, float frameRate)
        {
            var clip = new AnimationClip { frameRate = frameRate, name = name };
            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            float step = 1f / frameRate;
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i * step, value = frames[i] };
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length * step, value = frames[0] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static void SaveClip(AnimationClip clip, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
        }

        private static void BuildController(AnimationClip idle, AnimationClip move, AnimationClip run)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(ParamSpeed, AnimatorControllerParameterType.Float);
            controller.AddParameter(ParamShooting, AnimatorControllerParameterType.Bool);

            var sm = controller.layers[0].stateMachine;
            var idleState = sm.AddState("Idle");
            idleState.motion = idle;
            var moveState = sm.AddState("Move");
            moveState.motion = move;
            var runState = sm.AddState("RunShoot");
            runState.motion = run;
            sm.defaultState = idleState;

            AddTransition(idleState, moveState, (AnimatorConditionMode.Greater, 0.1f, ParamSpeed));
            AddTransition(moveState, idleState, (AnimatorConditionMode.Less, 0.1f, ParamSpeed));
            AddTransition(moveState, runState, (AnimatorConditionMode.If, 0f, ParamShooting));
            AddTransition(runState, moveState, (AnimatorConditionMode.IfNot, 0f, ParamShooting));
            AddTransition(idleState, runState,
                (AnimatorConditionMode.If, 0f, ParamShooting),
                (AnimatorConditionMode.Greater, 0.1f, ParamSpeed));
            AddTransition(runState, idleState,
                (AnimatorConditionMode.IfNot, 0f, ParamShooting),
                (AnimatorConditionMode.Less, 0.1f, ParamSpeed));
        }

        private static void AddTransition(AnimatorState from, AnimatorState to,
            params (AnimatorConditionMode mode, float threshold, string param)[] conds)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0f;
            foreach (var c in conds) t.AddCondition(c.mode, c.threshold, c.param);
        }
    }
}
