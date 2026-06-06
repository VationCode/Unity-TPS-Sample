using UnityEngine;
using UnityEngine.AI;

public static class Utility
{
    /// <summary>
    /// 중심에 위치한 반경거리까지 areaMask에 해당하는 네브메시에서의 랜덤한 위치를 반환
    /// </summary>
    /// <param name="areaMask"></param>
    public static Vector3 GetRandomPointOnNavMesh(Vector3 center, float distance, int areaMask)
    {
        var randomPos = Random.insideUnitSphere * distance + center;
        
        NavMeshHit hit;
        
        NavMesh.SamplePosition(randomPos, out hit, distance, areaMask);
        
        return hit.position;
    }
    
    public static float GedRandomNormalDistribution(float mean, float standard)
    {
        var x1 = Random.Range(0f, 1f);
        var x2 = Random.Range(0f, 1f);
        return mean + standard * (Mathf.Sqrt(-2.0f * Mathf.Log(x1)) * Mathf.Sin(2.0f * Mathf.PI * x2));
    }
}