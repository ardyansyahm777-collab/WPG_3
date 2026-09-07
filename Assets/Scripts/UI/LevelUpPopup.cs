using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using WpgGame.Core;
using WpgGame.Progression;

namespace WpgGame.UI
{
    /// <summary>
    /// Popup level-up: tampilkan LastRoll sebagai tombol, klik -> ChooseUpgrade + kembali Playing.
    /// </summary>
    public class LevelUpPopup : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceTemplate;

        private UpgradeSystem upgradeSystem;

        private void OnEnable()
        {
            upgradeSystem = FindFirstObjectByType<UpgradeSystem>();

            if (upgradeSystem != null)
            {
                upgradeSystem.OnLevelUp += HandleLevelUp;
            }

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (upgradeSystem != null)
            {
                upgradeSystem.OnLevelUp -= HandleLevelUp;
            }
        }

        private void HandleLevelUp()
        {
            GameManager.Instance?.SetState(GameManager.GameState.LevelUp);

            var roll = upgradeSystem != null ? upgradeSystem.LastRoll : null;
            if (roll == null || roll.Count == 0)
            {
                GameManager.Instance?.SetState(GameManager.GameState.Playing);
                return;
            }

            // Bersihkan container kecuali template.
            if (choicesContainer != null && choiceTemplate != null)
            {
                foreach (Transform child in choicesContainer)
                {
                    if (child.gameObject != choiceTemplate.gameObject)
                    {
                        Destroy(child.gameObject);
                    }
                }

                choiceTemplate.gameObject.SetActive(false);
            }

            GameObject firstSpawned = null;

            foreach (var opt in roll)
            {
                if (opt == null)
                {
                    continue;
                }

                Button btn = Instantiate(choiceTemplate, choicesContainer);
                btn.gameObject.SetActive(true);

                TMP_Text[] texts = btn.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length >= 2)
                {
                    texts[0].text = opt.DisplayName;
                    texts[1].text = opt.Description;
                }
                else if (texts.Length == 1)
                {
                    texts[0].text = opt.DisplayName;
                }

                var captured = opt;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnChoiceClicked(captured));

                if (firstSpawned == null)
                {
                    firstSpawned = btn.gameObject;
                }
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (firstSpawned != null)
            {
                EventSystem.current?.SetSelectedGameObject(firstSpawned);
            }
        }

        private void OnChoiceClicked(UpgradeOption opt)
        {
            if (upgradeSystem != null)
            {
                upgradeSystem.ChooseUpgrade(opt);
            }

            GameManager.Instance?.SetState(GameManager.GameState.Playing);

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
}
