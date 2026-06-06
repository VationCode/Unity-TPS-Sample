using UnityEngine;

public class PlayerHealth : LivingEntity
{
    private Animator _anim;
    private AudioSource _playerAudioPlayer;

    public AudioClip DeathClip;
    public AudioClip HitClip;

    [SerializeField]
    private float _maxHealth = 200;
    private void Awake()
    {
        _playerAudioPlayer = GetComponent<AudioSource>();
        _anim = GetComponentInChildren<Animator>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateUI();
    }
    
    public override void RestoreHealth(float newHealth)
    {
        if (_health + newHealth <= _maxHealth)
        {
            base.RestoreHealth(newHealth);
        }
        else
        {
            _health = _maxHealth;
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        UIManager.Instance.UpdateHealthText(IsDead ? 0f : _health);
    }
    
    public override bool ApplyDamage(DamageMessage damageMessage)
    {
        if (!base.ApplyDamage(damageMessage)) return false;
        EffectManager.Instance.PlayHitEffect(damageMessage.hitPoint,
            damageMessage.hitNormal, transform, EffectManager.EEffectType.Flesh);
        _playerAudioPlayer.PlayOneShot(HitClip);

        UpdateUI();

        return true;
    }
    
    public override void Die()
    {
        base.Die();
        _playerAudioPlayer.PlayOneShot(DeathClip);
        _anim.SetTrigger("Die");
        UpdateUI();
    }
}