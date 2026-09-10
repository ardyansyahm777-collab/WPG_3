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

        [Header("Heroes (toggle CharacterSelect, Play tetap ada)")]
        [SerializeField] private Button heroesButton;
        [SerializeField] private CharacterSelectController characterSelect;

        private void Awake()
        {
            if (titleText != null && string.IsNullOrEmpty(titleText.text))
            {
                titleText.text = "Bhagas naksir Dipta";
            }

            if (playButton != null)
            {
                playButton.onClick.RemoveListener(Play);
                playButton.onClick.AddListener(Play);
            }

            if (characterSelect == null)
            {
                characterSelect = FindFirstObjectByType<CharacterSelectController>();
            }
        }

        private void OnEnable()
        {
            if (heroesButton != null)
            {
                heroesButton.onClick.RemoveListener(ToggleHeroes);
                heroesButton.onClick.AddListener(ToggleHeroes);
            }

            if (characterSelect != null && characterSelect.IsShowing)
            {
                return;
            }

            if (playButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(playButton.gameObject);
            }
        }

        private void OnDisable()
        {
            if (heroesButton != null)
            {
                heroesButton.onClick.RemoveListener(ToggleHeroes);
            }
        }

        private void ToggleHeroes()
        {
            if (characterSelect == null)
            {
                characterSelect = FindFirstObjectByType<CharacterSelectController>();
            }

            if (characterSelect == null)
            {
                return;
            }

            characterSelect.ToggleSelect();

            if (!characterSelect.IsShowing && heroesButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(heroesButton.gameObject);
            }
        }

        public void Play()
        {
            SceneManager.LoadScene("Main");
        }
    }
}
