# API Contracts — Prototipe WPG_3 (Archero-like)

Dokumen ini adalah **kontrak API** yang harus dipatuhi semua programmer.
Field/method publik di bawah ini **tidak boleh berubah** tanpa diskusi di tim (mailbox/decision log).

---

## 1. Combat — `WpgGame.Combat.HealthSystem`

Komponen reusable untuk HP. Dipakai oleh Player dan Enemy.

```
[RequireComponent(typeof(...))] // opsional
public class HealthSystem : MonoBehaviour
{
    [SerializeField] public float MaxHP;
    public float CurrentHP { get; }
    public bool IsDead { get; }

    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action OnDeath;

    public void TakeDamage(float amount);
    public void Heal(float amount);    // opsional di slice awal
}
```

---

## 2. Player — `WpgGame.Player.PlayerStats`

```
public class PlayerStats : MonoBehaviour
{
    public float MoveSpeed;
    public float FireRate;     // shots per detik
    public float Damage;
    public int MultiShot;      // jumlah proyektil per tembakan

    public event Action<PlayerStats> OnStatsChanged;

    // WAJIB ada — dipanggil UpgradeSystem (Prog 2)
    public void ApplyUpgrade(WpgGame.Progression.UpgradeType type, float value);
}
```

---

## 3. Player — `WpgGame.Player.PlayerController`

Input via `InputSystem` Action `Player/Move` (Vector2).

```
public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction; // drag dari InputSystem_Actions
    // Implementasi: baca moveAction.ReadValue<Vector2>() di Update -> Rigidbody2D.linearVelocity
}
```

---

## 4. Combat — `WpgGame.Combat.Projectile`

```
public class Projectile : MonoBehaviour
{
    public float Damage;
    public float Lifetime;     // despawn setelah N detik
    public float Speed;
    public LayerMask HitMask;  // layer musuh

    public void Launch(Vector2 direction);
}
```

---

## 5. Enemy — `WpgGame.Enemy.EnemyAI`

```
public class EnemyAI : MonoBehaviour
{
    public float MoveSpeed;
    public float ContactDamage; // damage dealt on touch
    public HealthSystem Health; // reference

    public event Action<EnemyAI> OnEnemyDefeated; // opsional
}
```

---

## 6. Enemy — `WpgGame.Enemy.EnemySpawner`

```
public class EnemySpawner : MonoBehaviour
{
    public GameObject EnemyPrefab;
    public float SpawnInterval;     // detik antar spawn
    public int WaveSize;
    public Transform Player;        // spawn di sekitar player

    public void StartSpawning();
    public void StopSpawning();

    public event Action<int> OnWaveSpawned; // (jumlahMusuhSpawned)
}
```

---

## 7. Progression — `WpgGame.Progression.UpgradeType`

```
public enum UpgradeType
{
    MaxHealth,
    MoveSpeed,
    FireRate,
    Damage,
    MultiShot
}
```

---

## 8. Progression — `WpgGame.Progression.UpgradeOption`

ScriptableObject. Buat minimal 5 asset di `Assets/Data/UpgradeOptions/`.

```
[CreateAssetMenu(fileName = "UpgradeOption", menuName = "WPG_3/Upgrade Option")]
public class UpgradeOption : ScriptableObject
{
    public string Id;            // mis. "damage_plus"
    public string DisplayName;   // mis. "Damage +1"
    public string Description;
    public Sprite Icon;          // opsional
    public UpgradeType Type;
    public float Value;          // nilai yang ditambahkan (atau multiplier, disepakati = additive)
}
```

---

## 9. Progression — `WpgGame.Progression.UpgradeSystem`

```
public class UpgradeSystem : MonoBehaviour
{
    public UpgradeOption[] Pool;        // pool upgrade yang bisa dipilih
    public int ChoicesPerLevelUp;       // default 3

    public event Action OnLevelUp;      // UI subscribe -> tampilkan popup
    public event Action<UpgradeOption> OnUpgradeApplied;

    public UpgradeOption[] RollChoices();   // return acak N pilihan
    public void ChooseUpgrade(UpgradeOption choice);
}
```

---

## 10. Progression — `WpgGame.Progression.LevelUpSystem`

```
public class LevelUpSystem : MonoBehaviour
{
    public float CurrentExp { get; }
    public int CurrentLevel { get; }
    public float ExpToNextLevel { get; }

    public event Action<int> OnLevelChanged;   // (newLevel)
    public event Action<float, float> OnExpChanged; // (current, toNext)

    public void AddExperience(float amount);
}
```

---

## 11. Economy — `WpgGame.Economy.GoldManager`

```
public class GoldManager : MonoBehaviour
{
    public int CurrentGold { get; }
    public event Action<int> OnGoldChanged;
    public void Add(int amount);
    public bool TrySpend(int amount); // untuk nanti (shop/chest)
}
```

---

## 12. UI — `WpgGame.UI.HUDController`

Subscribe ke event. **Tidak** boleh panggil method internal sistem lain.

```
public class HUDController : MonoBehaviour
{
    // References via inspector (TMP_Text, Slider)
    public void Bind(WpgGame.Combat.HealthSystem playerHealth,
                    WpgGame.Progression.LevelUpSystem levelSystem,
                    WpgGame.Economy.GoldManager gold);
}
```

---

## 13. Input — Pakai asset bawaan `Assets/Settings/InputSystem_Actions.inputactions`

Action map & action yang dipakai:
- `Player/Move` (Value, Vector2) — gerak player
- `Player/Attack` (Button) — opsional, default auto-shoot (jika di-bind pun, AutoAimShooter yang akan handle)
- `UI/Submit`, `UI/Cancel` — untuk navigasi popup (jika perlu)

**Control scheme**: `KeyboardMouse` + `Touch` (sudah ada di asset). Tidak perlu buat asset baru.

---

## Aturan tambahan

- Subscribe event di `OnEnable`, unsubscribe di `OnDisable`. Wajib.
- Pakai `using UnityEngine.InputSystem;` untuk akses `InputAction`, `InputActionReference`.
- Pakai `UnityEngine.EventSystems` untuk event UI, **bukan** legacy `UnityEngine.UI` standalone.
- Untuk UI text, gunakan **TextMeshPro** (`TMP_Text`, `TMP_InputField`, dst).
- Rigidbody2D: pakai `linearVelocity` (bukan `velocity`, yang sudah deprecated di Unity 6).
