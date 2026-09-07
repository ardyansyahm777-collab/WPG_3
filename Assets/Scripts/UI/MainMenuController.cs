using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

namespace WpgGame.UI
{
    /// <summary>
    /// Menu utama: judul + tombol Play ke scene "Main".
    /// Tanpa referensi GameManager/GameEvents — menu tanpa gameplay.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text titleText;

        private void Awake()
        {
            if (titleText != null && string.IsNullOrEmpty(titleText.text))
            {
                titleText.text = "WPG_3";
            }

            if (playButton != null)
            {
                playButton.onClick.RemoveListener(Play);
                playButton.onClick.AddListener(Play);
            }
        }

        private void OnEnable()
        {
            if (playButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(playButton.gameObject);
            }
        }

        public void Play()
        {
            SceneManager.LoadScene("Main");
        }
    }
}
