using UnityEngine;
using WpgGame.Core;

namespace WpgGame.Economy
{
    /// <summary>
    /// FASE 1: pickup gold yang di-drop saat enemy mati.
    /// Perilaku magnet sama dengan ExpGem; collect grant via GoldManager (null-guard wajib).
    /// </summary>
    public class GoldCoin : MonoBehaviour
    {
        [SerializeField] private int goldAmount = 5;
        [SerializeField] private float magnetRadius = 3f;
        [SerializeField] private float collectRadius = 0.6f;
        [SerializeField] private float moveSpeed = 6f;

        public int GoldAmount
        {
            get => goldAmount;
            set => goldAmount = value;
        }

        public float MagnetRadius
        {
            get => magnetRadius;
            set => magnetRadius = value;
        }

        public float CollectRadius
        {
            get => collectRadius;
            set => collectRadius = value;
        }

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        private Transform _player;

        private static Sprite _cachedSprite;

        private void Awake()
        {
            EnsureVisual();
        }

        private void Update()
        {
            if (_player == null && !TryFindPlayer())
                return;

            var gm = GameManager.Instance;
            if (gm != null && gm.State != GameManager.GameState.Playing)
                return;

            Vector3 target = _player.position;
            float dist = Vector3.Distance(transform.position, target);

            if (dist <= collectRadius)
            {
                Collect();
                return;
            }

            if (dist <= magnetRadius)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, target) <= collectRadius)
                    Collect();
            }
        }

        private void Collect()
        {
            GoldManager.Instance?.Add(goldAmount);
            Destroy(gameObject);
        }

        private bool TryFindPlayer()
        {
            try
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null)
                {
                    _player = go.transform;
                    return true;
                }
            }
            catch (UnityException)
            {
                // Tag belum terdaftar; abaikan, coba lagi frame berikut.
            }
            return false;
        }

        private void EnsureVisual()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetOrCreateCoinSprite();
            renderer.sortingOrder = 2;

            var col = GetComponent<CircleCollider2D>();
            if (col == null)
                col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
        }

        private static Sprite GetOrCreateCoinSprite()
        {
            if (_cachedSprite != null)
                return _cachedSprite;

            _cachedSprite = CreateCircleSprite(new Color(1f, 0.85f, 0.2f, 1f), "goldcoin_circle_yellow");
            return _cachedSprite;
        }

        private static Sprite CreateCircleSprite(Color color, string spriteName)
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float radius = size * 0.5f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = d <= radius ? color : Color.clear;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = spriteName;
            return sprite;
        }
    }
}
