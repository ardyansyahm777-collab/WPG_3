#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WpgGame.EditorTools
{
    /// <summary>
    /// Helper editor untuk memastikan Main.unity punya GameObject GameManager dan Camera standar.
    /// Jalankan manual via menu: Tools > WPG_3 > Bootstrap Main Scene.
    /// Aman dijalankan berulang kali (idempotent).
    /// </summary>
    public static class SceneBootstrap
    {
        [MenuItem("Tools/WPG_3/Bootstrap Main Scene")]
        public static void BootstrapMain()
        {
            var scenePath = "Assets/Scenes/Main.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 1. GameManager
            var existingGM = Object.FindFirstObjectByType<WpgGame.Core.GameManager>();
            if (existingGM == null)
            {
                var go = new GameObject("[GameManager]");
                go.AddComponent<WpgGame.Core.GameManager>();
                Debug.Log("[SceneBootstrap] GameManager dibuat.");
            }

            // 2. Mark scene dirty lalu save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneBootstrap] Main.unity siap. Path: " + scenePath);
        }
    }
}
#endif
