using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using WpgGame.Player;

namespace WpgGame.UI
{
    /// <summary>
    /// Kartu hero kecil di grid CardSelectPanel.
    /// Bind(data, selected, onClick): set TMP nama/role/badge + portrait Image.
    /// Tidak panggil sistem lain — murni tampil + teruskan klik.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CharacterCard : MonoBehaviour
    {
        [Header("Refs (assign di prefab)")]
        [SerializeField] private Button cardButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text portraitFallbackText;
        [SerializeField] private TMP_Text badgeText;
        [SerializeField] private GameObject selectedHighlight;

        public Button CardButton => cardButton;

        private void Awake()
        {
            if (cardButton == null)
            {
                cardButton = GetComponent<Button>();
            }

            if (selectedHighlight != null)
            {
                selectedHighlight.SetActive(false);
            }
        }

        /// <summary>
        /// Isi kartu dari CharacterData. Locked tetap bisa diklik (preview),
        /// penolakan lock terjadi di SelectAndPlay, bukan di sini.
        /// </summary>
        public void Bind(CharacterData data, bool selected, UnityAction onClick)
        {
            if (data == null)
            {
                return;
            }

            if (cardButton == null)
            {
                cardButton = GetComponent<Button>();
            }

            string title = !string.IsNullOrEmpty(data.DisplayName)
                ? data.DisplayName
                : (!string.IsNullOrEmpty(data.Id) ? data.Id : "Hero");

            if (nameText != null)
            {
                nameText.text = title;
            }

            if (roleText != null)
            {
                roleText.text = string.IsNullOrEmpty(data.Role) ? "-" : data.Role;
            }

            if (portraitImage != null)
            {
                // API baru tanpa Sprite: sembunyikan Image, pakai teks inisial.
                portraitImage.enabled = false;
            }

            if (portraitFallbackText != null)
            {
                portraitFallbackText.text = !string.IsNullOrEmpty(data.PortraitText)
                    ? data.PortraitText
                    : (title.Length > 0 ? title.Substring(0, 1).ToUpper() : "?");
            }

            if (badgeText != null)
            {
                bool locked = !data.IsUnlocked;
                badgeText.text = locked ? "LOCKED" : string.Empty;
                badgeText.gameObject.SetActive(locked);
            }

            if (selectedHighlight != null)
            {
                selectedHighlight.SetActive(selected);
            }

            gameObject.name = "Card_" + title;

            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                if (onClick != null)
                {
                    cardButton.onClick.AddListener(onClick);
                }
            }
        }
    }
}
