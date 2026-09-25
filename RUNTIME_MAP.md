# RUNTIME_MAP — WPG_3: hierarchy play-mode → sumber kode

> Aturan emas: **jangan edit objek play-mode di Hierarchy** — tidak tersimpan.
> Ubah sumbernya (kode/prefab), lalu Stop → Play lagi.
> Cara cepat: di Editor jalankan `Tools/WPG_3/Log Runtime Source`, atau klik
> objek saat Play dan baca komponen `RuntimeSpawnTag` (field `SourceScript`).

| Hierarchy (play-mode) | Sumber kode | Prefab / asset | Edit / Runtime | Catatan |
|---|---|---|---|---|
| `[GameManager]` | `Assets/Scripts/Core/SceneSetup.cs` → `EnsureGameManager` | dibuat via kode (`GameManager.cs`, `DontDestroyOnLoad`) | Runtime | Singleton state `Playing/Paused/LevelUp/GameOver` |
| `[EventSystem]` | `SceneSetup.cs` → `EnsureEventSystem` | kode + `Assets/Settings/InputSystem_Actions.inputactions` | Runtime | `InputSystemUIInputModule`, `AssignDefaultActions` bila refs null |
| `[Main Camera]` + `CameraFollow` | `SceneSetup.cs` → `EnsureMainCamera` | kode (ortho 5.4) | Runtime | Target di-bind `GameplaySetup.BindCameraToPlayer` |
| `Player` | `Assets/Scripts/Player/GameplaySetup.cs` → `CreatePlayer` | kode (placeholder sprite putih) / `Assets/Prefabs/Player.prefab` bila lead integrasi | Runtime | Komponen: `PlayerController`, `PlayerStats`, `HeroSkill`, `AutoAimShooter`, `HealthSystem` |
| `EnemyTemplate` (inactive) | `GameplaySetup.cs` → `CreateEnemyTemplate` | kode | Runtime template | Tidak dirender; dipakai `EnemySpawner` via pool |
| `ProjectileTemplate` (inactive) | `GameplaySetup.cs` → `CreateProjectileTemplate` | kode | Runtime template | Dipakai `AutoAimShooter` via pool |
| `[EnemySpawner]` | `GameplaySetup.cs` → `OnSceneLoaded` / `MenuBootstrap` | kode | Runtime | `EnemyPrefab` + `Player` di-wire otomatis; `StartSpawning` hormati `GameManager.State` |
| `[Pool] Enemy/Projectile` + anaknya | `Assets/Scripts/Core/PrefabPool.cs` → `GetOrCreate/CreateInstance/Get` | prefab yang di-pool | Runtime | `Spawn` ganti `Instantiate`, `PooledObject.Release` ganti `Destroy`; pool per-scene (restart bersih) |
| Musuh hidup (`Enemy…`) | `Assets/Scripts/Enemy/EnemySpawner.cs` → `SpawnWave` via `PrefabPool` | `Enemy.prefab` / template | Runtime | Cap `MaxAliveEnemies=40`; cari via `EnemyAI`, bukan via Hierarchy |
| Proyektil hidup | `AutoAimShooter` via `PrefabPool` | `Projectile.prefab` / template | Runtime | `HitMask` layer `PG_Enemy` |
| `MenuUI/CardSelectPanel/CardGrid/Card_*` | `Assets/Scripts/UI/CharacterSelectController.cs` → `RenderCards` | `CardTemplate` (dibangun `CharacterSelectBootstrap`) | Runtime (diadopsi persisten) | Kartu manual di `CardGrid` diadopsi sebagai persisten, tidak dihancurkan |
| `DetailScroll/Viewport/DetailContent/Row_*` | `CharacterSelectController.cs` → `AddRow/AddParagraph` | `Assets/Prefabs/UI/StatRow.prefab`, `ParaRow.prefab` | Runtime | **Isi `DetailContent` SELALU kosong di edit-mode** — lahir dari kode + prefab; style di prefab Project, bukan di Hierarchy play-mode |
| `Showcase_*/DetailScroll/...` | `CharacterSelectController.cs` → `RenderShowcase` | panel per-hero / legacy `Center` | Runtime | `ClearDetails` tiap ganti hero/tab |
| Tombol LevelUp di `choicesContainer` | `Assets/Scripts/UI/LevelUpPopup.cs` → `HandleLevelUp` | `choiceTemplate` di `LevelUpPopup.prefab` | Runtime | Dibersihkan tiap roll; template dinonaktifkan |
| `HUD`, `GameOverScreen`, `LevelUpPopup` | scene `Main.unity` / prefab | `Assets/Prefabs/UI/HUD.prefab`, `GameOverScreen.prefab`, `LevelUpPopup.prefab` | Edit-mode (refs) | Subscribe event di `OnEnable`, unsubscribe `OnDisable` |
| `MenuUI/MainMenu/...` | scene `Main Menu.unity` + `Assets/Editor/CharacterSelectBootstrap.cs` (`Build Character Select UI`) | scene | Edit-mode | Idempoten; re-run aman; `CardSelectPanel`/`ShowcasePanel` nonaktif saat simpan |

## Cara melacak objek asing di play-mode

1. Play → klik objek di Hierarchy → Inspector → `RuntimeSpawnTag.SourceScript`.
2. Atau `Tools/WPG_3/Log Runtime Source` → baca Console (`infer=` bila belum ter-tag).
3. Ubah kode/prefab sumbernya (bukan objek play-mode) → Stop → Play lagi.
4. Validasi: `Tools/WPG_3/Validate Slice` → `[SliceCheck] ... PASS`, scene tersimpan bersih.
