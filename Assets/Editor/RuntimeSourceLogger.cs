#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using WpgGame.Core;

namespace WpgGame.EditorTools
{
    /// <summary>
    /// Tooling read-only: petakan hierarchy yang terlihat (termasuk objek runtime
    /// saat play-mode) ke sumber kode/prefab. Tidak membuat/menghapus objek.
    /// Menu: Tools/WPG_3/Log Runtime Source. Lihat RUNTIME_MAP.md.
    /// </summary>
    public static class RuntimeSourceLogger
    {
        [MenuItem("Tools/WPG_3/Log Runtime Source")]
        public static void LogRuntimeSource()
        {
            var scene = SceneManager.GetActiveScene();
            Debug.Log("[RuntimeSource] Scene: " + scene.name + " (isPlaying=" + Application.isPlaying + ")");
            Debug.Log("[RuntimeSource] Nama | Tag.SourceScript | Inferensi | Active | Scene");
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                LogTree(roots[i], 0);
            }
            Debug.Log("[RuntimeSource] Selesai. Objek runtime (play-mode) tidak bisa diedit permanen — ubah sumbernya, lihat RUNTIME_MAP.md.");
        }

        private static void LogTree(GameObject go, int depth)
        {
            if (go == null || depth > 2) return;
            var tag = go.GetComponent<RuntimeSpawnTag>();
            string source = tag != null ? tag.SourceScript : string.Empty;
            string prefab = tag != null ? tag.PrefabPath : string.Empty;
            string inferred = string.IsNullOrEmpty(source) ? InferSource(go.name) : source;
            string indent = new string(' ', depth * 2);
            Debug.Log("[RuntimeSource] " + indent + go.name
                + " | src=" + (string.IsNullOrEmpty(source) ? "-" : source)
                + (string.IsNullOrEmpty(prefab) ? string.Empty : " | prefab=" + prefab)
                + " | infer=" + inferred
                + " | active=" + go.activeSelf
                + " | scene=" + (go.scene.IsValid() ? go.scene.name : "?"));
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                if (child != null) LogTree(child.gameObject, depth + 1);
            }
        }

        private static string InferSource(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown";
            if (name.StartsWith("[GameManager]")) return "WpgGame.Core.SceneSetup.EnsureGameManager";
            if (name.StartsWith("[EventSystem]")) return "WpgGame.Core.SceneSetup.EnsureEventSystem";
            if (name.StartsWith("[Main Camera]")) return "WpgGame.Core.SceneSetup.EnsureMainCamera";
            if (name.StartsWith("[EnemySpawner]")) return "WpgGame.Player.GameplaySetup.OnSceneLoaded";
            if (name.StartsWith("[Pool]")) return "WpgGame.Core.PrefabPool.GetOrCreate";
            if (name.StartsWith("Row_")) return "WpgGame.UI.CharacterSelectController.AddRow/AddParagraph (prefab StatRow/ParaRow)";
            if (name.StartsWith("Card_") || name == "CardTemplate") return "WpgGame.UI.CharacterSelectController.RenderCards";
            if (name.Contains("Template")) return "WpgGame.Player.GameplaySetup.Create*Template (inactive)";
            if (name.StartsWith("Player")) return "WpgGame.Player.GameplaySetup.CreatePlayer";
            if (name.StartsWith("Enemy")) return "WpgGame.Enemy.EnemySpawner via PrefabPool";
            if (name.StartsWith("Projectile")) return "WpgGame.Player.AutoAimShooter via PrefabPool";
            if (name.StartsWith("[")) return "runtime bootstrap (kode, bukan edit-mode)";
            return "edit-mode atau spawn ter-tag (cek Inspector RuntimeSpawnTag)";
        }
    }
}
#endif
