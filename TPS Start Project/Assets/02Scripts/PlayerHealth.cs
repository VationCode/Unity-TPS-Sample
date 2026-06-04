using UnityEngine;

public class PlayerHealth : LivingEntity
{
    private Animator _anim;
    private AudioSource _playerAudioPlayer;
    private CharacterController _characterController;

    public AudioClip DeathClip;
    public AudioClip HitClip;


    private void Awake()
    {
        _playerAudioPlayer = GetComponent<AudioSource>();
        _anim = GetComponentInChildren<Animator>();
        _characterController = GetComponent<CharacterController>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateUI();
    }
    
    public override void RestoreHealth(float newHealth)
    {
        base.RestoreHealth(newHealth);
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