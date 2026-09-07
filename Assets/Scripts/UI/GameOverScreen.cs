using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using WpgGame.Core;

namespace WpgGame.UI
{
    /// <summary>
    /// Layar game-over: tampil saat GameEvents.OnGameOver, tombol restart -> GameManager.Restart().
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button restartButton;
        [SerializeField] private TMP_Text titleText;

        private void Awake()
        {
            if (titleText != null && string.IsNullOrEmpty(titleText.text))
            {
                titleText.text = "GAME OVER";
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGameOver += HandleGameOver;

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= HandleGameOver;
        }

        private void HandleGameOver()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (restartButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(restartButton.gameObject);
            }
        }

        public void OnRestartClicked()
        {
            GameManager.Instance?.Restart();
        }
    }
}
