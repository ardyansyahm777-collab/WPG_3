using UnityEngine;

namespace WpgGame.UI
{
    /// <summary>
    /// Animasi layang naik-turun (sinus) untuk RectTransform.
    /// Aman-layout: pasang pada objek yang TIDAK dikontrol LayoutGroup
    /// (mis. Label di dalam Button), JANGAN pada tombol di dalam ButtonRow —
    /// layout akan menimpa anchoredPosition setiap rebuild.
    /// Posisi dasar dicatat di OnEnable setelah layout selesai (Start() berjalan
    /// sebelum layout pass pertama sehingga nilainya basi).
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatingObject : MonoBehaviour
    {
        [Tooltip("Jarak layang maksimal (unit kanvas).")]
        [SerializeField] private float amplitude = 10f;

        [Tooltip("Kecepatan osilasi.")]
        [SerializeField] private float speed = 1.5f;

        private RectTransform rectTransform;
        private Vector2 baseAnchoredPos;
        private bool hasBase;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (rectTransform == null)
            {
                enabled = false;
                return;
            }

            Canvas.ForceUpdateCanvases();
            baseAnchoredPos = rectTransform.anchoredPosition;
            hasBase = true;
        }

        private void Update()
        {
            if (!hasBase || rectTransform == null)
            {
                return;
            }

            float y = baseAnchoredPos.y + Mathf.Sin(Time.unscaledTime * speed) * amplitude;
            rectTransform.anchoredPosition = new Vector2(baseAnchoredPos.x, y);
        }

        private void OnDisable()
        {
            if (hasBase && rectTransform != null)
            {
                rectTransform.anchoredPosition = baseAnchoredPos;
            }
        }
    }
}
