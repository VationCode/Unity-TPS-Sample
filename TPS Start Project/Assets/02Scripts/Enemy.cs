using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class Enemy : LivingEntity
{
    private enum EState
    {
        IDle,
        Patrol,
        Tracking,
        AttackBegin,
        Attacking
    }

    public bool CanPtrolZombie;
    private EState _state;
    
    private NavMeshAgent _agent;
    private Animator _anim;

    public Transform AttackRoot;
    public Transform EyeTransform;
    
    private AudioSource _audioPlayer;
    public AudioClip IdleClip;
    public AudioClip ChaseClip;
    public AudioClip AttackClip;
    public AudioClip HitClip;
    public AudioClip DeathClip;
    
    private Renderer _skinRenderer;

    public float RunSpeed = 10f;
    [Range(0.01f, 2f)] public float TurnSmoothTime = 0.1f;
    private float _turnSmoothVelocity;
    
    public float Damage = 30f;
    public float AttackRadius = 0.5f;
    private float _attackDistance;
    
    public float FieldOfView = 50f;
    public float ViewDistance = 10f;
    public float _patrolSpeed = 3f;
    
    public LivingEntity TargetEntity;
    public LayerMask WhatIsTarget;


    private RaycastHit[] _hits = new RaycastHit[10];
    private List<LivingEntity> _lastAttackedTargets = new List<LivingEntity>();
    
    private bool _hasTarget => TargetEntity != null && !TargetEntity.IsDead;
    

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if(AttackRoot != null)
        {
            Gizmos.color = new Color(1f,0f,0f,0.5f);
            Gizmos.DrawSphere(AttackRoot.position, AttackRadius);
        }

        if (EyeTransform != null)
        {
            // 기준 축으로부터 왼쪽으로 fieldOfView의 반만큼의 각도(범위가 60도면 -30도만큼)
            var leftEyeRotation = Quaternion.AngleAxis(-FieldOfView * 0.5f, Vector3.up);
            // -30도의 방향
            var leftRayDirection = leftEyeRotation * transform.forward;
            Handles.color = new Color(1f, 1f, 1f, 0.2f);
            // 범위만큼 호를 그림
            Handles.DrawSolidArc(EyeTransform.position, Vector3.up, leftRayDirection, FieldOfView, ViewDistance);
        }
    }
    
#endif
    
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        _audioPlayer = GetComponent<AudioSource>();
        _skinRenderer = GetComponentInChildren<Renderer>();

        var attackPivot = AttackRoot.position;
        attackPivot.y = transform.position.y;
        _attackDistance = Vector3.Distance(transform.position, attackPivot) + AttackRadius;
        
        _agent.stoppingDistance = _attackDistance;
        _agent.speed = _patrolSpeed;
    }

    public void Setup(float p_health, float p_damage,
        float p_runSpeed, float p_patrolSpeed, Color p_skinColor)
    {
        this._startingHealth = p_health;
        this._health = p_health;

        this.Damage = p_damage;
        this.RunSpeed = p_runSpeed;
        this._patrolSpeed = p_patrolSpeed;

        _skinRenderer.material.color = p_skinColor;

        _agent.speed = p_patrolSpeed;
    }

    private void Start()
    {
        Initialize();
        StartCoroutine(UpdatePath());
    }

    private void Update()
    {
        if (IsDead) return;

        if(_state == EState.Tracking)
        {
            if(TargetEntity == null || TargetEntity.IsDead)
            {
                Initialize();
                return;
            }

            if(_audioPlayer.clip != ChaseClip)
            {
                _anim.SetTrigger("Chase");
                _audioPlayer.clip = ChaseClip;
                _audioPlayer.Play();
            }
            var distance = Vector3.Distance(TargetEntity.transform.position, transform.position);

            if(distance <= _attackDistance)
            {
                if (_audioPlayer.loop == true)
                {
                    _audioPlayer.loop = false;
                }
                BeginAttack();
            }
        }

    }

    private void FixedUpdate()
    {
        if (IsDead) return;
        if(TargetEntity == null) return;
        // 공격하려는 대상을 바라보게 회전
        if(_state == EState.AttackBegin || _state == EState.Attacking)
        {
            var lookRotation = Quaternion.LookRotation(TargetEntity.transform.position - transform.position);
            var targetAngleY = lookRotation.eulerAngles.y;

            targetAngleY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngleY, ref _turnSmoothVelocity, TurnSmoothTime);

            transform.eulerAngles = Vector3.up * targetAngleY;
        }

        if(_state == EState.Attacking)
        {
            var direction = transform.forward;
            var deltaDis = _agent.velocity.magnitude * Time.fixedDeltaTime;

            // _hits를 직접 선언함으로써 새로 계속 만들지 않기에 메모리 관리
            var size = Physics.SphereCastNonAlloc(AttackRoot.position, AttackRadius, direction, _hits, deltaDis, WhatIsTarget);

            for(var i = 0; i< size; i++)
            {
                var attackTragetEntity = _hits[i].collider.GetComponent<LivingEntity>();

                if(attackTragetEntity != null && !_lastAttackedTargets.Contains(attackTragetEntity))
                {
                    var message = new DamageMessage();
                    message.amount = Damage;
                    message.damager = gameObject;

                    // 공격 휘두르기전 이미 겹친 콜라이더가 있다면 _hits[i].point는 제로가 나옴 
                    // 어택 루트를 포인트로
                    if (_hits[i].distance <= 0f)
                    {
                        message.hitPoint = AttackRoot.position;
                    }
                    else // 휘두르는도중 콜라이더 감지시
                    {
                        message.hitPoint = _hits[i].point;
                    }

                    message.hitNormal = _hits[i].normal;

                    attackTragetEntity.ApplyDamage(message);
                    _lastAttackedTargets.Add(attackTragetEntity);
                    break;
                }
            }
        }
    }

    private IEnumerator UpdatePath()
    {
        while (!IsDead)
        {
            if (_hasTarget)
            {
                if (_state == EState.Patrol || _state == EState.IDle)
                {
                    _state = EState.Tracking;
                    _agent.speed = RunSpeed;
                }
                _agent.SetDestination(TargetEntity.transform.position);
            }
            else
            {
                if (TargetEntity != null) TargetEntity = null;

                // 타겟이 복수라면
                if (CanPtrolZombie)
                {
                    if (_state != EState.Patrol)
                    {
                        _state = EState.Patrol;
                        _agent.speed = _patrolSpeed;

                        _audioPlayer.loop = true;
                        _audioPlayer.clip = IdleClip;
                        _audioPlayer.Play();
                    }

                    // 패트롤 장소까지 1정도 남았을경우에만 새로 다시 패트롤 위치 랜덤하게
                    if (_agent.remainingDistance <= 1f)
                    {
                        var patroltargetPos = Utility.GetRandomPointOnNavMesh(transform.position, 20f, NavMesh.AllAreas);
                        _agent.SetDestination(patroltargetPos);
                    }
                }
                var colliders = Physics.OverlapSphere(EyeTransform.position, ViewDistance, WhatIsTarget);
                foreach(var collider in colliders)
                {
                    // 감지 중 대상과 중간에 물체가 있는지 확인하기
                    if (!IsTargetOnSight(collider.transform))
                    {
                        continue;
                    }

                    // 감지한 대상이 생명체이면
                    var livingEntity = collider.GetComponent<LivingEntity>();
                    // 쫒아가야한다면 순찰 마침
                    if(livingEntity != null && !livingEntity.IsDead)
                    {
                        TargetEntity = livingEntity;
                        break;
                    }
                }
            }
            
            yield return new WaitForSeconds(0.05f);
        }
    }
    
    public override bool ApplyDamage(DamageMessage damageMessage)
    {
        if (!base.ApplyDamage(damageMessage)) return false;
        
        // 공격한 대상을 타겟으로
        if(TargetEntity == null)
        {
            TargetEntity = damageMessage.damager.GetComponent<LivingEntity>();
           
        }

        EffectManager.Instance.PlayHitEffect(damageMessage.hitPoint, damageMessage.hitNormal, transform, EffectManager.EEffectType.Flesh);

        _audioPlayer.PlayOneShot(HitClip);
        return true;
    }

    public void BeginAttack()
    {
        _state = EState.AttackBegin;
        _audioPlayer.PlayOneShot(AttackClip);
        _agent.isStopped = true;
        _anim.SetTrigger("Attack");
    }

    // 애니메이션에서 호출
    public void EnableAttack()
    {
        _state = EState.Attacking;
        _lastAttackedTargets.Clear();
        Debug.Log("EnableAttack");
    }
    private void Initialize()
    {
        if (CanPtrolZombie)
        {
            _state = EState.Patrol;
            _anim.SetTrigger("Patrol");
        }
        else
        {
            _state = EState.IDle;
            _anim.SetTrigger("Idle");
        }

        _audioPlayer.Stop();
        _audioPlayer.clip = IdleClip;
        _audioPlayer.loop = true;
        _audioPlayer.Play();
    }
    public void DisableAttack()
    {
        if(_hasTarget) _state = EState.Tracking;
        else
        {
            Initialize();
        }

        _agent.isStopped = false;
        Debug.Log("DisableAttack");
    }

    // 시야내에 존재하는지 체크
    private bool IsTargetOnSight(Transform target)
    {
        var direction = target.position - EyeTransform.position;
        
        // 수평만 검사하기 위해
        direction.y = EyeTransform.forward.y;

        // 정면 기준 왼쪽(30)이든 오른쪽(30)이든 범위를 벗어났을 경우
        if (Vector3.Angle(direction, EyeTransform.forward) > FieldOfView * 0.5f)
            return false;

        direction = target.position - EyeTransform.position;

        RaycastHit hit;
        // 타겟과 Enemy사이 사물있는지 체크
        if (Physics.Raycast(EyeTransform.position, direction, out hit, ViewDistance, WhatIsTarget))
        {
            if(hit.transform == target)
            {
                return true;
            }
        }

        return false;
    }
    
    public override void Die()
    {
        base.Die();

        GetComponent<Collider>().enabled = false;
        
        // isStop같은걸로하게되면 에이전트들은 서로 피해감
        _agent.enabled = false;

        _anim.applyRootMotion = true;
        _anim.SetTrigger("Die");

        _audioPlayer.PlayOneShot(DeathClip);
    }
}