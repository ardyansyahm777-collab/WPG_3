#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace WpgGame.Player
{
    /// <summary>
    /// Setup editor untuk gameplay: memastikan nama layer PG_Player / PG_Enemy / PG_Projectile
    /// dan tag "Player" terdaftar di ProjectSettings/TagManager.asset (slot user layer kosong pertama).
    /// Idempotent - aman dijalankan berkali-kali. Dipakai HitMask Projectile dan pemisahan collision.
    /// </summary>
    [InitializeOnLoad]
    public static class GameplayEditorSetup
    {
        private const string PlayerLayer = "PG_Player";
        private const string EnemyLayer = "PG_Enemy";
        private const string ProjectileLayer = "PG_Projectile";

        static GameplayEditorSetup()
        {
            // Dijalankan setiap kali assembly rampung direload (editor start / recompile).
            EditorApplication.delayCall += EnsureLayersAndTags;
        }

        [MenuItem("Tools/WPG_3/Ensure Gameplay Layers & Tags")]
        public static void EnsureLayersAndTags()
        {
            var targets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (targets == null || targets.Length == 0) return;

            foreach (var target in targets)
            {
                var so = new SerializedObject(target);

                var layers = so.FindProperty("layers");
                if (layers != null)
                {
                    int slot = 8;
                    SlotLayer(layers, PlayerLayer, ref slot);
                    SlotLayer(layers, EnemyLayer, ref slot);
                    SlotLayer(layers, ProjectileLayer, ref slot);
                }

                var tags = so.FindProperty("tags");
                if (tags != null) EnsureTag(tags, "Player");

                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SlotLayer(SerializedProperty layers, string name, ref int slot)
        {
            if (layers == null) return;

            for (int i = 8; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == name) return;
            }

            while (slot < layers.arraySize)
            {
                var element = layers.GetArrayElementAtIndex(slot);
                if (string.IsNullOrWhiteSpace(element.stringValue))
                {
                    element.stringValue = name;
                    slot++;
                    return;
                }
                slot++;
            }

            Debug.LogWarning("[WPG_3] Tidak ada slot layer user kosong untuk " + name + " (buat manual di Project Settings > Tags and Layers).");
        }

        private static void EnsureTag(SerializedProperty tags, string tagName)
        {
            if (tags == null) return;

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tagName) return;
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
        }
    }
}
#endif
