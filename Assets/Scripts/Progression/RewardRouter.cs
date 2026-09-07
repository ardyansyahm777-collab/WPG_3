using UnityEngine;
using WpgGame.Core;
using WpgGame.Economy;

namespace WpgGame.Progression
{
    /// <summary>
    /// FASE 1: subscribe OnEnemyKilled, spawn drop ExpGem + GoldCoin di posisi enemy mati.
    /// Komunikasi murni via GameEvents; tidak menyentuh sistem lain langsung.
    /// </summary>
    public class RewardRouter : MonoBehaviour
    {
        [SerializeField] private float expPerKill = 10f;
        [SerializeField] private int goldPerKill = 5;

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(GameObject enemy)
        {
            if (enemy == null)
                return;

            Vector3 basePos = enemy.transform.position;
            SpawnExpGem(basePos);
            SpawnGoldCoin(basePos);
        }

        private void SpawnExpGem(Vector3 basePos)
        {
            var go = new GameObject("ExpGem");
            go.transform.position = basePos + (Vector3)Random.insideUnitCircle * 0.3f;
            var gem = go.AddComponent<ExpGem>();
            gem.ExpAmount = expPerKill;
        }

        private void SpawnGoldCoin(Vector3 basePos)
        {
            var go = new GameObject("GoldCoin");
            go.transform.position = basePos + (Vector3)Random.insideUnitCircle * 0.3f;
            var coin = go.AddComponent<GoldCoin>();
            coin.GoldAmount = goldPerKill;
        }
    }
}
