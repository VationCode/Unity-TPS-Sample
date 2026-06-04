using System;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public enum EState
    {
        Ready,
        Empty,
        Reloading
    }
    public EState State { get; private set; }
    
    private PlayerShooter _gunHolder;
    private LineRenderer _bulletLineRenderer;

    [SerializeField]
    private AudioSource _gunAudioPlayer;
    public AudioClip ShotClip;
    public AudioClip ReloadClip;
    
    public ParticleSystem MuzzleFlashEffect;
    public ParticleSystem ShellEjectEffect;
    
    public Transform FireTransform;
    public Transform LeftHandMount;

    public float Damage = 25;
    public float FireDistance = 100f;

    public int AmmoRemain = 100;
    public int MagAmmo;
    public int MagCapacity = 30;

    public float TimeBetFire = 0.12f;
    public float ReloadTime = 1.2f;
    
    // 퍼짐
    [Range(0f, 10f)] public float MaxSpread = 3f;
    // 안정감(반동 잡는)
    [Range(1f, 10f)] public float Stability = 1f;
    [Range(0.01f, 3f)] public float RestoreFromRecoilSpeed = 2f;
    private float _currentSpread;
    private float _currentSpreadVelocity;

    [SerializeField]
    private float _rate = 0.1f;
    private float _lastFireTime;

    private LayerMask _excludeTarget;

    private void Awake()
    {
        _gunAudioPlayer = GetComponent<AudioSource>();
        _bulletLineRenderer = GetComponent<LineRenderer>();

        _bulletLineRenderer.positionCount = 2;
        _bulletLineRenderer.enabled = false;
    }

    public void Setup(PlayerShooter p_gunHolder)
    {
        this._gunHolder = p_gunHolder;
        // 총을 쏘지 않을 타겟(레이캐스트가 안되는)
        _excludeTarget = p_gunHolder.ExcludeTarget;
    }

    private void OnEnable()
    {
        MagAmmo = MagCapacity;
        _currentSpread = 0f;
        _lastFireTime = 0f;
        State = EState.Ready;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public bool Fire(Vector3 p_aimTarget)
    {
        // 발사 가능 상태 체크
        if (State == EState.Ready && Time.time >= _lastFireTime)
        {
            var fireDir = p_aimTarget - FireTransform.position;

            // 정규분포에 의한 탄 퍼짐
            // _currentSpread가 높을수록 0과 차이가 많이나는 값이 들어올 확률이 높음
            var xError = Utility.GedRandomNormalDistribution(0, _currentSpread);
            var yError = Utility.GedRandomNormalDistribution(0, _currentSpread);

            // 발사 위치에서 살짝 다른각도로 회전하여 발사
            fireDir = Quaternion.AngleAxis(yError, Vector3.up) * fireDir;
            fireDir = Quaternion.AngleAxis(xError, Vector3.right) * fireDir;

            // 다음 반동 증가 및 정확도 내려가게
            // Stability가 커질수록 퍼짐 줄어듬
            _currentSpread += 1f / Stability;

            _lastFireTime = Time.time + _rate;
            Shot(FireTransform.position, fireDir);

            return true;
        }

        return false;
    }
    
    private void Shot(Vector3 p_startPoint, Vector3 p_direction)
    {
        RaycastHit hit;
        Vector3 hitPosition;
        
        if (Physics.Raycast(p_startPoint, p_direction, out hit, FireDistance, ~_excludeTarget))
        {
            var target = hit.collider.GetComponent<IDamageable>();

            if (target != null)
            {
                DamageMessage damageMessage;

                damageMessage.damager = _gunHolder.gameObject;
                damageMessage.amount = Damage;
                damageMessage.hitPoint = hit.point;
                damageMessage.hitNormal = hit.normal;

                target.ApplyDamage(damageMessage);
            }
            else
            {
                Debug.Log("NotTarget");
                EffectManager.Instance.PlayHitEffect(hit.point, hit.normal, hit.transform);
            }
            hitPosition = hit.point;
        }
        else
        {
            hitPosition = p_startPoint + p_direction * FireDistance;
        }

        StartCoroutine(ShotEffect(hitPosition));

        MagAmmo--;
        if (MagAmmo <= 0) State = EState.Empty;
    }

    private IEnumerator ShotEffect(Vector3 p_hitPosition)
    {
        MuzzleFlashEffect.Play();
        ShellEjectEffect.Play();
        
        _gunAudioPlayer.PlayOneShot(ShotClip);

        _bulletLineRenderer.enabled = true;
        _bulletLineRenderer.SetPosition(0, FireTransform.position);
        _bulletLineRenderer.SetPosition(1, p_hitPosition);

        // 텀이 있어야 라인랜더러 바로 안꺼지니 궤적 그려짐
        yield return new WaitForSeconds(0.03f);

        _bulletLineRenderer.enabled = false;
    }
    
    public bool Reload()
    {
        if(State == EState.Reloading || AmmoRemain <= 0 || MagAmmo >= MagCapacity)
        {
            return false;
        }

        StartCoroutine(ReloadRoutine());
        
        return true;
    }

    private IEnumerator ReloadRoutine()
    {
        State = EState.Reloading;
        _gunAudioPlayer.PlayOneShot(ReloadClip);

        yield return new WaitForSeconds(ReloadTime);

        // 현재 탄창에 장전 가능한 탄약 수 계산
        // 이 때 장전 가능한 수는 0발에서 현재 가지고 있는 AmmoRemain(예비 탄약) 수만큼이 최소 최대이기에 제약
        var ammoToFill = Mathf.Clamp(MagCapacity - MagAmmo, 0, AmmoRemain);
        
        // 장전
        MagAmmo += ammoToFill;
        // 예비 탄약에 장전 수 만큼 제거
        AmmoRemain -= ammoToFill;

        State = EState.Ready;
    }

    private void Update()
    {
        _currentSpread = Mathf.Clamp(_currentSpread, 0, MaxSpread);

        // 매프레임 0에 가까워지도록
        _currentSpread = 
            Mathf.SmoothDamp(_currentSpread, 0f, ref _currentSpreadVelocity, 1f/RestoreFromRecoilSpeed);
    }
}