using UnityEngine;

namespace WpgGame.Core
{
    /// <summary>
    /// Runtime bootstrap: jika scene belum punya GameManager / EventSystem / Camera setup standar,
    /// script ini menambahkannya otomatis saat scene dimuat.
    /// Memastikan prototipe tetap jalan walaupun programmer lupa taruh prefab di scene.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    public class SceneSetup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureGameManager();
            EnsureEventSystem();
            EnsureMainCamera();
        }

        private static void EnsureGameManager()
        {
            if (GameManager.Instance != null) return;
            var go = new GameObject("[GameManager]");
            go.AddComponent<GameManager>();
        }

        private static void EnsureEventSystem()
        {
            var existing = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existing != null) return;
            var go = new GameObject("[EventSystem]");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // Pakai InputSystemUIInputModule agar cocok dengan paket com.unity.inputsystem.
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private static void EnsureMainCamera()
        {
            var cam = Camera.main;
            if (cam != null) return;
            var go = new GameObject("[Main Camera]");
            go.tag = "MainCamera";
            var c = go.AddComponent<Camera>();
            c.orthographic = true;
            c.orthographicSize = 5.4f; // ~1080p / 200 pixels-per-unit-ish, fine untuk prototipe landscape
            c.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 1f);
            c.clearFlags = CameraClearFlags.SolidColor;
            go.AddComponent<AudioListener>();
        }
    }
}
