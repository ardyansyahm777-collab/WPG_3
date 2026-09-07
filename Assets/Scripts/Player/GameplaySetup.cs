using UnityEngine;
using UnityEngine.SceneManagement;
using WpgGame.Combat;
using WpgGame.Core;
using WpgGame.Enemy;

namespace WpgGame.Player
{
    /// <summary>
    /// Runtime bootstrap khusus gameplay: memastikan tersedia Player, template musuh &amp; proyektil,
    /// dan EnemySpawner saat scene dimuat - mirip mekanisme SceneSetup di Core.
    /// HANYA membuat object bila belum ada (aman saat lead integrasi memakai prefab asli).
    /// Template placeholder dibuat inactive supaya tidak ikut render/ber-interaksi di scene.
    /// </summary>
    public static class GameplaySetup
    {
        private const string GameplaySceneName = "Main";

        private static GameObject _player;
        private static HealthSystem _subscribedHealth;
        private static GameObject _enemyTemplate;
        private static GameObject _projectileTemplate;

        private static Sprite _whiteSprite;
        private static Sprite _redSprite;
        private static Sprite _yellowSprite;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/WPG_3/Bootstrap Gameplay Objects")]
        private static void MenuBootstrap()
        {
            // Panggil Helpers sehingga object dibuat langsung di scene aktif (mode edit).
            GetOrCreatePlayer();
            GetOrCreateEnemyTemplate();
            GetOrCreateProjectileTemplate();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner == null)
            {
                var go = new GameObject("[EnemySpawner]");
                spawner = go.AddComponent<EnemySpawner>();
            }
            if (spawner.EnemyPrefab == null) spawner.EnemyPrefab = GetOrCreateEnemyTemplate();
            if (spawner.Player == null && _player != null) spawner.Player = _player.transform;
            var shooter = _player != null ? _player.GetComponent<AutoAimShooter>() : null;
            if (shooter != null) shooter.ProjectilePrefab = GetOrCreateProjectileTemplate();
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[GameplaySetup] Bootstrap gameplay objects selesai (Player, Enemy template, Projectile template, Spawner).");
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameplay()
        {
            if (SceneManager.GetActiveScene().name != GameplaySceneName) return;

            var player = GetOrCreatePlayer();

            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner == null)
            {
                var go = new GameObject("[EnemySpawner]");
                spawner = go.AddComponent<EnemySpawner>();
            }
            if (spawner.EnemyPrefab == null) spawner.EnemyPrefab = GetOrCreateEnemyTemplate();
            if (spawner.Player == null && player != null) spawner.Player = player.transform;
            spawner.StartSpawning();
        }

        public static GameObject GetOrCreatePlayer()
        {
            if (_player == null)
            {
                var existing = Object.FindFirstObjectByType<PlayerController>();
                _player = existing != null ? existing.gameObject : CreatePlayer();
            }
            EnsurePlayerDeathSubscription(_player);
            return _player;
        }

        private static void EnsurePlayerDeathSubscription(GameObject player)
        {
            if (player == null) return;
            var health = player.GetComponent<HealthSystem>();
            if (health == null) return;
            if (_subscribedHealth == health) return;
            if (_subscribedHealth != null)
            {
                _subscribedHealth.OnDeath -= HandleSubscribedPlayerDeath;
            }
            health.OnDeath += HandleSubscribedPlayerDeath;
            _subscribedHealth = health;
        }

        public static GameObject GetOrCreateEnemyTemplate()
        {
            if (_enemyTemplate == null) _enemyTemplate = CreateEnemyTemplate();
            return _enemyTemplate;
        }

        public static GameObject GetOrCreateProjectileTemplate()
        {
            if (_projectileTemplate == null) _projectileTemplate = CreateProjectileTemplate();
            return _projectileTemplate;
        }

        private static GameObject CreatePlayer()
        {
            var go = new GameObject("Player",
                typeof(Rigidbody2D),
                typeof(CircleCollider2D),
                typeof(SpriteRenderer),
                typeof(HealthSystem),
                typeof(PlayerStats),
                typeof(PlayerController),
                typeof(AutoAimShooter));

            go.layer = ResolveLayer("PG_Player");
            TrySetTag(go, "Player");

            var sprite = GetOrCreateSolidSprite(ref _whiteSprite, Color.white);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 0;

            var rb = go.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = go.GetComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = false;

            var health = go.GetComponent<HealthSystem>();
            health.MaxHP = 3f;

            var stats = go.GetComponent<PlayerStats>();
            stats.MoveSpeed = 5f;
            stats.FireRate = 3f;
            stats.Damage = 1f;
            stats.MultiShot = 1;

            return go;
        }

        private static GameObject CreateEnemyTemplate()
        {
            var go = new GameObject("EnemyTemplate",
                typeof(Rigidbody2D),
                typeof(BoxCollider2D),
                typeof(SpriteRenderer),
                typeof(HealthSystem),
                typeof(EnemyAI));

            go.layer = ResolveLayer("PG_Enemy");

            var sprite = GetOrCreateSolidSprite(ref _redSprite, new Color(0.85f, 0.2f, 0.2f, 1f));
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 0;

            var rb = go.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = go.GetComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);

            var health = go.GetComponent<HealthSystem>();
            health.MaxHP = 2f;

            var ai = go.GetComponent<EnemyAI>();
            ai.MoveSpeed = 2f;
            ai.ContactDamage = 1f;

            go.SetActive(false);
            return go;
        }

        private static GameObject CreateProjectileTemplate()
        {
            var go = new GameObject("ProjectileTemplate",
                typeof(Rigidbody2D),
                typeof(CircleCollider2D),
                typeof(SpriteRenderer),
                typeof(Projectile));

            go.layer = ResolveLayer("PG_Projectile");

            var sprite = GetOrCreateSolidSprite(ref _yellowSprite, new Color(1f, 0.85f, 0.2f, 1f));
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 1;

            var rb = go.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.GetComponent<CircleCollider2D>();
            col.radius = 0.15f;
            col.isTrigger = true;

            var projectile = go.GetComponent<Projectile>();
            projectile.Damage = 1f;
            projectile.Lifetime = 3f;
            projectile.Speed = 10f;
            int enemyLayer = ResolveLayer("PG_Enemy");
            projectile.HitMask = enemyLayer >= 0 ? 1 << enemyLayer : ~0;

            go.SetActive(false);
            return go;
        }

        private static void HandleSubscribedPlayerDeath()
        {
            HandlePlayerDeath(_player, _subscribedHealth);
        }

        private static void HandlePlayerDeath(GameObject player, HealthSystem health)
        {
            GameEvents.RaisePlayerDied(player);
            var gm = GameManager.Instance;
            if (gm != null && gm.State != GameManager.GameState.GameOver)
            {
                gm.SetState(GameManager.GameState.GameOver);
            }
        }

        private static Sprite GetOrCreateSolidSprite(ref Sprite cache, Color color)
        {
            if (cache != null) return cache;

            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();

            cache = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            cache.name = "placeholder_" + color.ToString();
            return cache;
        }

        private static int ResolveLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? layer : 0;
        }

        private static void TrySetTag(GameObject target, string tag)
        {
            if (target == null) return;
            try
            {
                target.tag = tag;
            }
            catch
            {
                // tag belum terdaftar di TagManager; sistem gameplay tidak bergantung pada tag.
            }
        }
    }
}


