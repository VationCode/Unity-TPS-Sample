using System;
using UnityEngine;

public class LivingEntity : MonoBehaviour, IDamageable
{
    public float _startingHealth = 100f;
    public float _health { get; protected set; }
    public bool IsDead { get; protected set; }
    
    public event Action OnDeath;
    
    // 다음 공격 받을 수 있는 최소 시간
    private const float minTimeBetDamaged = 0.1f;
    private float lastDamagedTime;

    protected bool IsInvulnerabe
    {
        get
        {
            if (Time.time >= lastDamagedTime + minTimeBetDamaged) return false;

            return true;
        }
    }
    
    protected virtual void OnEnable()
    {
        IsDead = false;
        _health = _startingHealth;
    }

    public virtual bool ApplyDamage(DamageMessage damageMessage)
    {
        if (IsInvulnerabe || damageMessage.damager == gameObject || IsDead) return false;

        lastDamagedTime = Time.time;
        _health -= damageMessage.amount;

        if (_health <= 0)
        {
            _health = 0;
            Die();
        }

        return true;
    }
    
    public virtual void RestoreHealth(float newHealth)
    {
        if (IsDead) return;
        
        _health += newHealth;
    }
    
    public virtual void Die()
    {
        if (OnDeath != null) OnDeath();
        
        IsDead = true;
    }
}