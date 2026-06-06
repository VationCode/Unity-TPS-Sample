using System.Collections.Generic;
using UnityEngine;

// 적 게임 오브젝트를 주기적으로 생성
public class EnemySpawner : MonoBehaviour
{
    private readonly List<Enemy> enemies = new List<Enemy>();

    public float DamageMax = 40f;
    public float DamageMin = 20f;
    public Enemy[] EnemyPrefabs;

    public float HealthMax = 200f;
    public float HealthMin = 100f;

    public Transform[] SpawnPoints;

    public float SpeedMax = 3f;
    public float SpeedMin = 1f;

    public Color StrongEnemyColor = Color.red;
    private int _wave;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameover) return;
        
        if (enemies.Count <= 0) SpawnWave();
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        UIManager.Instance.UpdateWaveText(_wave, enemies.Count);
    }
    
    private void SpawnWave()
    {
        _wave++;

        var spawnCount = Mathf.RoundToInt(_wave * 5f);

        for(var i =0; i< spawnCount; i++)
        {
            // 강함정도
            var enemyIntensity = Random.Range(0f, 1f);

            CreateEnemy(enemyIntensity);
        }
    }
    
    private void CreateEnemy(float p_intensity)
    {
        var health = Mathf.Lerp(HealthMin, HealthMax, p_intensity);
        var damage = Mathf.Lerp(DamageMin, DamageMax, p_intensity);
        //var speed = Mathf.Lerp(SpeedMin, SpeedMax, p_intensity);

        var skinColor = Color.Lerp(Color.white, StrongEnemyColor, p_intensity);

        var spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Length)];

        var enemy = Instantiate(EnemyPrefabs[Random.Range(0,EnemyPrefabs.Length)], spawnPoint.position, spawnPoint.rotation);

        enemy.Setup(health, damage, enemy.RunSpeed, enemy._patrolSpeed, skinColor);

        enemies.Add(enemy);

        enemy.OnDeath += () => enemies.Remove(enemy);
        enemy.OnDeath += () => Destroy(enemy.gameObject, 10f);
        enemy.OnDeath += () => GameManager.Instance.AddScore(100);
    }
}