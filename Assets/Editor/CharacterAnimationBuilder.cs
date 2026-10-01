#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace WpgGame.EditorTools
{
    /// <summary>
    /// Membangun 3 animasi karakter Aelindra (idle_bow, running_bow, running_shoot_bow)
    /// masing-masing 5 frame dari file .aseprite di Assets/Gambar/Karakter/
    /// dan menyusun AnimatorController PlayerIdle.controller dengan transisi responsif.
    /// Menu: Tools > WPG_3 > Build Aelindra Animations & Controller
    /// </summary>
    [InitializeOnLoad]
    public static class CharacterAnimationBuilder
    {
        private const string IdleAsePath = "Assets/Gambar/Karakter/idle_bow.aseprite";
        private const string RunAsePath = "Assets/Gambar/Karakter/running_bow.aseprite";
        private const string RunShootAsePath = "Assets/Gambar/Karakter/running_shoot_bow.aseprite";

        private const string OutDir = "Assets/Data/Anims/";
        private const string IdleClipPath = "Assets/Data/Anims/PlayerIdle.anim";
        private const string RunClipPath = "Assets/Data/Anims/PlayerRun.anim";
        private const string RunShootClipPath = "Assets/Data/Anims/PlayerRunShoot.anim";
        private const string ControllerPath = "Assets/Data/Anims/PlayerIdle.controller";

        static CharacterAnimationBuilder()
        {
            // Jalankan sekali saat compile/load jika clip belum dibuat atau perlu dipastikan
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(RunClipPath) || !File.Exists(RunShootClipPath))
                {
                    BuildAllAnimations();
                }
            };
        }

        [MenuItem("Tools/WPG_3/Build Aelindra Animations & Controller")]
        public static void BuildAllAnimations()
        {
            Directory.CreateDirectory(OutDir);

            // 1. Ambil sprites (masing-masing 5 frame)
            var idleSprites = LoadFrames(IdleAsePath, 5);
            var runSprites = LoadFrames(RunAsePath, 5);
            var runShootSprites = LoadFrames(RunShootAsePath, 5);

            if (idleSprites == null || runSprites == null || runShootSprites == null)
            {
                Debug.LogError("[CharacterAnimationBuilder] Gagal memuat sprites dari file .aseprite.");
                return;
            }

            // 2. Buat AnimationClip (sampleRate 8 fps untuk animasi pixel-art 5-frame yang natural)
            var idleClip = CreateOrReplaceClip(IdleClipPath, idleSprites, 6f);
            var runClip = CreateOrReplaceClip(RunClipPath, runSprites, 8f);
            var runShootClip = CreateOrReplaceClip(RunShootClipPath, runShootSprites, 8f);

            // 3. Susun Animator Controller
            SetupAnimatorController(idleClip, runClip, runShootClip);

            // 4. Update default sprite di prefab Player jika ada
            UpdatePlayerPrefab(idleSprites[0]);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CharacterAnimationBuilder] SUKSES membuat 3 animasi 5-frame ({idleSprites.Length} idle, {runSprites.Length} run, {runShootSprites.Length} run_shoot) dan AnimatorController di {ControllerPath}!");
        }

        private static Sprite[] LoadFrames(string path, int frameCount)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError($"[CharacterAnimationBuilder] Tidak dapat membaca file asset di {path}");
                return null;
            }

            var sprites = assets.OfType<Sprite>()
                .OrderBy(s =>
                {
                    var match = Regex.Match(s.name, @"\d+");
                    return match.Success ? int.Parse(match.Value) : 0;
                })
                .Take(frameCount)
                .ToArray();

            if (sprites.Length == 0)
            {
                Debug.LogError($"[CharacterAnimationBuilder] Tidak ditemukan sub-sprite di {path}");
                return null;
            }

            return sprites;
        }

        private static AnimationClip CreateOrReplaceClip(string clipPath, Sprite[] sprites, float fps)
        {
            var clip = new AnimationClip { frameRate = fps };
            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };

            float step = 1f / fps;
            var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (int i = 0; i < sprites.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i * step, value = sprites[i] };
            }
            // Loop kembali ke frame 0 di akhir durasi
            keys[sprites.Length] = new ObjectReferenceKeyframe { time = sprites.Length * step, value = sprites[0] };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
            {
                AssetDatabase.DeleteAsset(clipPath);
            }

            AssetDatabase.CreateAsset(clip, clipPath);
            return clip;
        }

        private static void SetupAnimatorController(AnimationClip idleClip, AnimationClip runClip, AnimationClip runShootClip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            // Pastikan parameters ada
            EnsureBoolParameter(controller, "IsMoving");
            EnsureBoolParameter(controller, "IsShooting");

            var stateMachine = controller.layers[0].stateMachine;

            // Bersihkan state lama agar setup rapi dan deterministik
            foreach (var childState in stateMachine.states)
            {
                stateMachine.RemoveState(childState.state);
            }

            // Buat 3 States
            var idleState = stateMachine.AddState("idle_bow", new Vector3(200, 0, 0));
            idleState.motion = idleClip;

            var runState = stateMachine.AddState("running_bow", new Vector3(480, 0, 0));
            runState.motion = runClip;

            var runShootState = stateMachine.AddState("running_shoot_bow", new Vector3(340, 130, 0));
            runShootState.motion = runShootClip;

            stateMachine.defaultState = idleState;

            // Transisi:
            // 1. idle_bow -> running_bow (bergerak dan tidak menembak)
            var t1 = idleState.AddTransition(runState);
            t1.hasExitTime = false;
            t1.duration = 0f;
            t1.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            t1.AddCondition(AnimatorConditionMode.IfNot, 0, "IsShooting");

            // 2. running_bow -> idle_bow (berhenti dan tidak menembak)
            var t2 = runState.AddTransition(idleState);
            t2.hasExitTime = false;
            t2.duration = 0f;
            t2.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            t2.AddCondition(AnimatorConditionMode.IfNot, 0, "IsShooting");

            // 3. running_bow -> running_shoot_bow (sedang lari lalu menembak)
            var t3 = runState.AddTransition(runShootState);
            t3.hasExitTime = false;
            t3.duration = 0f;
            t3.AddCondition(AnimatorConditionMode.If, 0, "IsShooting");

            // 4. running_shoot_bow -> running_bow (selesai menembak tapi masih bergerak)
            var t4 = runShootState.AddTransition(runState);
            t4.hasExitTime = false;
            t4.duration = 0f;
            t4.AddCondition(AnimatorConditionMode.IfNot, 0, "IsShooting");
            t4.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");

            // 5. running_shoot_bow -> idle_bow (selesai menembak dan tidak bergerak)
            var t5 = runShootState.AddTransition(idleState);
            t5.hasExitTime = false;
            t5.duration = 0f;
            t5.AddCondition(AnimatorConditionMode.IfNot, 0, "IsShooting");
            t5.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            // 6. idle_bow -> running_shoot_bow (menembak saat idle)
            var t6 = idleState.AddTransition(runShootState);
            t6.hasExitTime = false;
            t6.duration = 0f;
            t6.AddCondition(AnimatorConditionMode.If, 0, "IsShooting");

            EditorUtility.SetDirty(controller);
        }

        private static void EnsureBoolParameter(AnimatorController controller, string paramName)
        {
            if (controller.parameters.Any(p => p.name == paramName)) return;
            controller.AddParameter(paramName, AnimatorControllerParameterType.Bool);
        }

        private static void UpdatePlayerPrefab(Sprite defaultSprite)
        {
            const string prefabPath = "Assets/Prefabs/Player.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            var sr = prefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && defaultSprite != null)
            {
                sr.sprite = defaultSprite;
                EditorUtility.SetDirty(prefab);
            }
        }
    }
}
#endif
