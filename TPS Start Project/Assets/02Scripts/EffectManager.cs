using UnityEngine;

// 이펙트 파티클의 경우 오브젝트 풀링으로 관리해야함
// 해당 프로젝트는 파티클 시스템에 동작이후 자동 삭제 설정이 되어있음
public class EffectManager : MonoBehaviour
{
    private static EffectManager m_Instance;
    public static EffectManager Instance
    {
        get
        {
            if (m_Instance == null) m_Instance = FindObjectOfType<EffectManager>();
            return m_Instance;
        }
    }
    // Common : 대부분의 이펙트
    // Felsh : 피부나 살에 총알이 부딪혔을때 등 생명체에
    public enum EEffectType
    {
        Common,
        Flesh
    }
    
    public ParticleSystem CommonHitEffectPrefab;
    public ParticleSystem FleshHitEffectPrefab;
    
    public void PlayHitEffect(Vector3 p_pos, Vector3 p_normal, Transform p_parent = null, EEffectType p_effectType = EEffectType.Common)
    {
        var targetPrefab = CommonHitEffectPrefab;

        if (p_effectType == EEffectType.Flesh)
        {
            targetPrefab = FleshHitEffectPrefab;
        }
        Debug.Log("Effect");
        var effect = Instantiate(targetPrefab, p_pos, Quaternion.LookRotation(p_normal));

        if(p_parent != null)
        {
            effect.transform.SetParent(p_parent);
        }

        effect.Play();
    }
}