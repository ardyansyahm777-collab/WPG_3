using TMPro;
using UnityEngine;
using WpgGame.Core;

namespace WpgGame.Combat
{
    /// <summary>
    /// Jenis tampilan popup damage: hit normal (putih), crit (kuning besar),
    /// atau damage yang diterima player (merah).
    /// </summary>
    public enum DamagePopupKind
    {
        Normal,
        Crit,
        Player,
    }

    /// <summary>
    /// Satu teks damage melayang (TextMeshPro world-space): naik + fade lalu
    /// kembali ke pool <see cref="DamagePopupManager"/>. Freeze saat
    /// GameManager.State bukan Playing (usia/gerak/alpha diam).
    /// Instance hanya dibuat lewat manager (pool), bukan di scene.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        private TextMeshPro _tmp;
        private DamagePopupManager _home;
        private float _age;
        private float _life = 0.7f;
        private Vector3 _velocity = Vector3.up * 1.1f;

        internal bool InUse { get; private set; }

        /// <summary>Siapkan komponen TMP sekali saat instance pool dibuat.</summary>
        public void Init(DamagePopupManager home)
        {
            _home = home;
            _tmp = gameObject.AddComponent<TextMeshPro>();
            _tmp.alignment = TextAlignmentOptions.Center;
            _tmp.enableAutoSizing = false;
            _tmp.richText = false;
            var rend = GetComponent<MeshRenderer>();
            if (rend != null) rend.sortingOrder = 30;
            gameObject.SetActive(false);
        }

        /// <summary>Tampilkan angka di posisi dunia dengan gaya sesuai jenis.</summary>
        public void Show(Vector3 worldPos, float amount, DamagePopupKind kind)
        {
            var home = _home;
            float normalSize = home != null ? home.NormalFontSize : 7f;
            float critSize = home != null ? home.CritFontSize : 10f;
            float playerSize = home != null ? home.PlayerFontSize : 8f;
            float life = home != null ? home.PopupLife : 0.7f;
            float riseSpeed = home != null ? home.RiseSpeed : 1.1f;

            _life = Mathf.Max(0.1f, life);
            _age = 0f;
            _velocity = Vector3.up * Mathf.Max(0f, riseSpeed);
            transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            transform.localScale = Vector3.one;
            InUse = true;

            // Font custom dari Inspector (null = font default TextMeshPro).
            if (home != null && home.DamageFont != null && _tmp.font != home.DamageFont)
                _tmp.font = home.DamageFont;

            _tmp.text = amount.ToString("0.#");
            switch (kind)
            {
                case DamagePopupKind.Crit:
                    _tmp.fontSize = critSize;
                    _tmp.color = home != null ? home.CritColor : new Color(1f, 0.85f, 0.2f);
                    _tmp.fontStyle = FontStyles.Bold;
                    _tmp.outlineWidth = 0.15f;
                    _tmp.outlineColor = Color.black;
                    transform.localScale = Vector3.one * 1.35f;
                    break;
                case DamagePopupKind.Player:
                    _tmp.fontSize = playerSize;
                    _tmp.color = home != null ? home.PlayerColor : new Color(1f, 0.3f, 0.25f);
                    _tmp.fontStyle = FontStyles.Bold;
                    _tmp.outlineWidth = 0.1f;
                    _tmp.outlineColor = Color.black;
                    break;
                default:
                    _tmp.fontSize = normalSize;
                    _tmp.color = home != null ? home.NormalColor : Color.white;
                    _tmp.fontStyle = FontStyles.Normal;
                    _tmp.outlineWidth = 0.08f;
                    _tmp.outlineColor = Color.black;
                    break;
            }
            _tmp.alpha = 1f;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!InUse) return;
            var gm = GameManager.Instance;
            if (gm != null && gm.State != GameManager.GameState.Playing) return;

            _age += Time.deltaTime;
            if (_age >= _life)
            {
                Release();
                return;
            }
            transform.position += _velocity * Time.deltaTime;
            // Fade di 40% akhir usia.
            float remain = _life - _age;
            float fadeWindow = _life * 0.4f;
            if (remain < fadeWindow) _tmp.alpha = Mathf.Clamp01(remain / fadeWindow);
        }

        private void Release()
        {
            InUse = false;
            if (_home != null) _home.Release(this);
            else gameObject.SetActive(false);
        }
    }
}
