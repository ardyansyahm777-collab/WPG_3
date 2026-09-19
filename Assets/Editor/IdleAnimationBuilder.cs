using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace WpgGame.Editor
{
    /// <summary>
    /// Tool: Tools &gt; WPG_3 &gt; Build Player Idle Animation.
    /// Merakit AnimationClip loop (sprite-swap) + AnimatorController dari frame idle_*
    /// di Assets/Gambar/Karakter/idle.png ke Assets/Data/Anims/. Rebuild deterministik
    /// (aset lama dihapus dulu). Tanpa dialog agar bisa dipanggil via CLI.
    /// </summary>
    public static class IdleAnimationBuilder
    {
        private const string TexturePath = "Assets/Gambar/Karakter/idle.png";
        private const string OutDir = "Assets/Data/Anims/";
        private const string ClipPath = "Assets/Data/Anims/PlayerIdle.anim";
        private const string ControllerPath = "Assets/Data/Anims/PlayerIdle.controller";

        [MenuItem("Tools/WPG_3/Build Player Idle Animation")]
        public static void BuildPlayerIdle()
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(TexturePath)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();
            if (sprites.Length == 0)
            {
                Debug.LogError($"[IdleBuilder] Tidak ada sub-sprite di {TexturePath}. " +
                               "Set Texture Type=Sprite, Sprite Mode=Multiple, lalu Slice.");
                return;
            }

            Directory.CreateDirectory(OutDir);

            var clip = new AnimationClip { frameRate = 6 };
            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            float step = 1f / clip.frameRate;
            var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (int i = 0; i < sprites.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i * step, value = sprites[i] };
            keys[sprites.Length] = new ObjectReferenceKeyframe { time = sprites.Length * step, value = sprites[0] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) != null)
                AssetDatabase.DeleteAsset(ClipPath);
            AssetDatabase.CreateAsset(clip, ClipPath);

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddMotion(clip);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[IdleBuilder] OK frames={sprites.Length} clip={ClipPath} controller={ControllerPath}");
        }
    }
}
