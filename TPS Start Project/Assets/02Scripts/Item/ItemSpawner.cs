using UnityEngine;
using UnityEngine.AI;

public class ItemSpawner : MonoBehaviour
{
    public GameObject[] Items;
    public Transform PlayerTransform;
    
    private float _lastSpawnTime;
    public float MaxDistance = 5f;
    
    private float _timeBetSpawn;

    public float TimeBetSpawnMax = 7f;
    public float TimeBetSpawnMin = 2f;

    private void Start()
    {
        _timeBetSpawn = Random.Range(TimeBetSpawnMin, TimeBetSpawnMax);
        _lastSpawnTime = 0f;
    }

    private void Update()
    {
        if(Time.time >= _lastSpawnTime + _timeBetSpawn && PlayerTransform != null)
        {
            Spawn();
            _lastSpawnTime = Time.time;
            _timeBetSpawn = Random.Range(TimeBetSpawnMin, TimeBetSpawnMax);
        }
    }

    private void Spawn()
    {
        var spawnPos = Utility.GetRandomPointOnNavMesh(PlayerTransform.position, MaxDistance, NavMesh.AllAreas);
        spawnPos += Vector3.up * 0.5f;

        var item = Instantiate(Items[Random.Range(0, Items.Length)], spawnPos, Quaternion.identity);
        Destroy(item, 5f);
    }
}