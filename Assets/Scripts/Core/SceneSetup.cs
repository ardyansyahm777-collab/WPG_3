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
            if (existing == null)
            {
                var go = new GameObject("[EventSystem]");
                go.AddComponent<UnityEngine.EventSystems.EventSystem>();
                // Pakai InputSystemUIInputModule agar cocok dengan paket com.unity.inputsystem.
                existing = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>()
                    .GetComponent<UnityEngine.EventSystems.EventSystem>();
            }
            EnsureUiActions(existing);
        }

        /// <summary>
        /// Pastikan module UI punya action refs yang hidup. Tanpa ini (asset terisi
        /// tapi action null, mis. prefab kehilangan sub-objek InputActionReference),
        /// HasNoActions() = false sehingga fallback default TIDAK jalan dan seluruh
        /// input UI mati total (hover/klik/keyboard). Aman di runtime & build
        /// (tanpa AssetDatabase: hanya memakai actionsAsset yang sudah terpasang).
        /// </summary>
        private static void EnsureUiActions(UnityEngine.EventSystems.EventSystem es)
        {
            var module = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (module == null) return;
            if (module.actionsAsset == null)
            {
                module.AssignDefaultActions();
                return;
            }
            var asset = module.actionsAsset;
            if (module.point == null || module.point.action == null)
                module.point = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/Point"));
            if (module.move == null || module.move.action == null)
                module.move = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/Navigate"));
            if (module.leftClick == null || module.leftClick.action == null)
                module.leftClick = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/Click"));
            if (module.submit == null || module.submit.action == null)
                module.submit = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/Submit"));
            if (module.cancel == null || module.cancel.action == null)
                module.cancel = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/Cancel"));
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
