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
        Idle,
        Patrol,
        Chase,
        AttackBegin,
        Attacking,
        Die
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
    public LayerMask ObstacleMask; // 장애물

    // 타겟들이 복수개일 경우를 대비
    private RaycastHit[] _hits = new RaycastHit[10];
    private List<LivingEntity> _lastAttackedTargets = new List<LivingEntity>();
    
    private bool _hasTarget => TargetEntity != null && !TargetEntity.IsDead;
    private bool _isAggro;
    [SerializeField]
    private float _loseTargetHeight = 3f;
    [SerializeField]
    private float _loseTargetDistance = 20f;
    [SerializeField]
    private float _loseTargetMaxDistance = 50f;
    [SerializeField]
    private float _loseSightTime = 3f;

    private float _lastSeenTime;
    private Vector3 _lastDestination;
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

    public void Setup(float p_health, float p_damage, float p_runSpeed, float p_patrolSpeed, Color p_skinColor)
    {
        this._startingHealth = p_health;
        this._health = p_health;

        this.Damage = p_damage;
        this.RunSpeed = p_runSpeed;
        this._patrolSpeed = p_patrolSpeed;

        _skinRenderer.material.color = p_skinColor;

        _agent.speed = p_patrolSpeed;
    }

    private void Initialize()
    {
        ChangeState(CanPtrolZombie ? EState.Patrol : EState.Idle);

        _agent.speed = _patrolSpeed;
    }
    private void Start()
    {
        Initialize();
        
        StartCoroutine(UpdatePath());
    }

    private IEnumerator UpdatePath()
    {
        while (!IsDead)
        {
            if (_hasTarget)
            {
                NavMeshHit hit;

                // 네비메시 끊겨 있는곳으로 플레이어가 도주 시를 위한 코드
                if (NavMesh.SamplePosition(TargetEntity.transform.position, out hit, 20f, NavMesh.AllAreas))
                {
                    if (Vector3.Distance(_lastDestination, hit.position) > 1f)
                    {
                        _lastDestination = hit.position;
                        _agent.SetDestination(hit.position);
                    }
                }
            }
            else // 타겟이 없을경우
            {
                if (CanPtrolZombie)
                {
                    // 패트롤 장소까지 1정도 남았을경우에만 새로 다시 패트롤 위치 랜덤하게
                    if (_agent.remainingDistance <= 1f)
                    {
                        var patroltargetPos = Utility.GetRandomPointOnNavMesh(transform.position, 20f, NavMesh.AllAreas);
                        _agent.SetDestination(patroltargetPos);
                    }
                }
                // 대상 감지
                DetectedTargetEntity();
            }
            yield return new WaitForSeconds(0.05f);
        }
    }
    private void FixedUpdate()
    {
        if (_state == EState.Die) return;

        // 공격시 대상을 바라보게 회전
        if (_state == EState.AttackBegin || _state == EState.Attacking)
        {
            LookTarget();
        }

        if (_state == EState.Attacking)
        {
            // 공격 피해 입은 대상 데미지 적용
            HitTargetTakesDamage();
        }
    }


    private void Update()
    {
        if (IsDead) return;

        switch (_state)
        {
            case EState.Chase:
                UpdateChase();
                break;
        }

    }
    private void LoseTarget()
    {
        TargetEntity = null;
        _isAggro = false;

        if (_agent.enabled)
            _agent.ResetPath();

        Initialize();
    }

    private void UpdateChase()
    {
        if (!_hasTarget)
        {
            LoseTarget();
            return;
        }

        if (!CheckSight()) return;
        if (!CheckDistance()) return;

        CheckAttackRange();
    }

    private bool CheckSight()
    {
        if (_isAggro) return true;

        if (IsTargetOnSight(TargetEntity.transform))
        {
            _lastSeenTime = Time.time;
            return true;
        }
        else if (Time.time - _lastSeenTime > _loseSightTime)
        {
            LoseTarget();
            return false;
        }
        return true;
    }
    private bool CheckDistance()
    {
        // 거리 체크
        float sqrDistance = (TargetEntity.transform.position - transform.position).sqrMagnitude;

        // 맞아서 발견과 시야로 발견에 따른 차이
        float loseDistance = _isAggro ? _loseTargetMaxDistance : _loseTargetDistance;

        if (sqrDistance > loseDistance * loseDistance)
        {
            LoseTarget();
            return false;
        }
        return true;
    }

    private void CheckAttackRange()
    {
        float sqrDistance = (TargetEntity.transform.position - transform.position).sqrMagnitude;

        if (sqrDistance <= _attackDistance * _attackDistance)
        {
            ChangeState(EState.AttackBegin);
        }
    }

    private void ChangeState(EState p_state)
    {
        switch (p_state)
        {
            case EState.Idle:
                EnterIdle();
                break;
            case EState.Patrol:
                EnterPatrol();
                break;
            case EState.Chase:
                EnterChase();
                break;
            case EState.AttackBegin:
                BeginAttack();
                break;
            case EState.Attacking:
                _state = EState.Attacking;
                break;
            case EState.Die:
                _state = EState.Die;
                break;
        }
    }

    private void SetStateAnim(string p_name)
    {
        foreach (var parameter in _anim.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool)
            {
                _anim.SetBool(parameter.name, false);
            }
        }

        _anim.SetBool(p_name, true);
    }

    private void SetAudio(bool p_isLoop, AudioClip p_clip, bool p_isOnShot = false)
    {
        _audioPlayer.loop = p_isLoop;

        if (p_isOnShot)
        {
            _audioPlayer.PlayOneShot(p_clip);
            return;
        }
        if (_audioPlayer.clip != p_clip)
        {
            _audioPlayer.Stop();
            _audioPlayer.clip = p_clip;
            _audioPlayer.Play();
        }
    }

    private void EnterIdle()
    {
        _state = EState.Idle;

        if (_agent.enabled)
            _agent.isStopped = true;

        SetStateAnim("Idle");
        SetAudio(true, IdleClip);
    }

    private void EnterPatrol()
    {
        _state = EState.Patrol;

        if (_agent.enabled)
            _agent.isStopped = false;

        SetStateAnim("Patrol");
        SetAudio(true, IdleClip);
    }
    private void EnterChase()
    {
        _state = EState.Chase;
        _lastSeenTime = Time.time;

        _agent.speed = RunSpeed;
        if (_agent.enabled)
            _agent.isStopped = false;

        SetStateAnim("Chase");
        SetAudio(true, ChaseClip);
    }

    private void DetectedTargetEntity()
    {
        TargetEntity = null;

        // 범위 WhatIsTarget들 감지
        var colliders = Physics.OverlapSphere(EyeTransform.position, ViewDistance, WhatIsTarget);

        foreach (var collider in colliders)
        {
            // 범위내 감지된 대상과 중간에 물체가 있는지 확인하기
            if (!IsTargetOnSight(collider.transform)) continue;

            // 감지한 대상이 생명체이면
            if (collider.TryGetComponent(out LivingEntity livingEntity))
            {
                if (livingEntity.IsDead) continue;

                // LivingEntity 확인 후
                TargetEntity = livingEntity;

                // 추적으로 상태 전환
                ChangeState(EState.Chase);
                break;
            }
        }
    }

    // 시야내에 존재하는지 체크
    private bool IsTargetOnSight(Transform target)
    {
        float heightDiff = Mathf.Abs(target.position.y - EyeTransform.position.y);

        if (heightDiff > 3f) return false;
        
        Vector3 targetPos = target.position + Vector3.up * 1.5f;
        Vector3 direction = targetPos - EyeTransform.position;

        // 정면 기준 왼쪽(30)이든 오른쪽(30)이든 범위를 벗어났을 경우
        if (Vector3.Angle(direction, EyeTransform.forward) > FieldOfView * 0.5f)
            return false;

        RaycastHit hit;
        // 타겟과 Enemy사이 사물있는지 체크
        if (!Physics.Raycast(EyeTransform.position, direction, out hit, ViewDistance, ObstacleMask))
        {
            return true;
        }
        return false;
    }

    #region ======================================= FixedUpdate
    private void LookTarget()
    {
        if (TargetEntity == null) return;
        var lookRotation = Quaternion.LookRotation(TargetEntity.transform.position - transform.position);
        var targetAngleY = lookRotation.eulerAngles.y;

        targetAngleY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngleY, ref _turnSmoothVelocity, TurnSmoothTime);

        transform.eulerAngles = Vector3.up * targetAngleY;
    }

    // 맞은 대상 데미지 전달
    private void HitTargetTakesDamage()
    {
        var direction = transform.forward;
        var deltaDis = _agent.velocity.magnitude * Time.fixedDeltaTime;

        // _hits를 직접 선언함으로써 새로 계속 만들지 않기에 메모리 관리
        // 실제로 해당 범위안에 있는(맞은) 대상들을 판별 쫒는 TargetEntity와 맞은 WhatIsTarget은 다를 수 있기에
        var size = Physics.SphereCastNonAlloc(AttackRoot.position, AttackRadius, direction, _hits, deltaDis, WhatIsTarget);

        for (var i = 0; i < size; i++)
        {
            var hitTragetEntity = _hits[i].collider.GetComponent<LivingEntity>();

            if (hitTragetEntity != null && !_lastAttackedTargets.Contains(hitTragetEntity))
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

                hitTragetEntity.ApplyDamage(message);
                _lastAttackedTargets.Add(hitTragetEntity);
                break;
            }
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
        _isAggro = true;
        ChangeState(EState.Chase);
        SetAudio(false, HitClip, true);
        return true;
    }
    #endregion

    public void BeginAttack()
    {
        _state = EState.AttackBegin;
        SetAudio(false, AttackClip, true);
        if (_agent.enabled)
            _agent.isStopped = true;
        _anim.SetTrigger("Attack");
    }

    // 애니메이션에서 호출
    public void EnableAttack()
    {
        ChangeState(EState.Attacking);
        _lastAttackedTargets.Clear();
    }
   
    public void DisableAttack()
    {
        if(_agent.enabled)
        _agent.isStopped = false;

        if (!_isAggro)
        {
            DetectedTargetEntity();
        }

        if (_hasTarget)
        {
            ChangeState(EState.Chase);
        }
        else
        {
            Initialize();
        }
    }

    public override void Die()
    {
        base.Die();

        GetComponent<Collider>().enabled = false;
        _state = EState.Die;
        // isStop같은걸로하게되면 에이전트들은 서로 피해감
        _agent.enabled = false;

        _anim.applyRootMotion = true;
        _anim.SetTrigger("Die");

        _audioPlayer.PlayOneShot(DeathClip);
    }
}