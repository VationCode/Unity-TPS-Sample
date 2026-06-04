using UnityEngine;

// 총 쏘는 기능
public class PlayerShooter : MonoBehaviour
{
    public enum EAimState
    {
        Idle,
        HipFire
    }

    public EAimState AimState { get; private set; }

    [HideInInspector] public Gun Gun;
    public LayerMask ExcludeTarget;
    
    private PlayerInput _playerInput;
    private Animator _playerAnimator;
    private Camera _playerCamera;

    // Aim 유지 시간
    private float _watingTimeForReleasingAim = 2.5f;
    private float _lastFireInputTime;

    // 실제 모델에서 발사해서 닿는 곳
    private Vector3 _aimPoint;
    private bool _isLinedUp => !(Mathf.Abs( _playerCamera.transform.eulerAngles.y - transform.eulerAngles.y) > 1f);
    private bool _hasEnoughDistance => !Physics.Linecast(transform.position + Vector3.up * Gun.FireTransform.position.y,Gun.FireTransform.position, ~ExcludeTarget);
    
    void Awake()
    {
        // 플레이어 스스로를 타겟으로 하지 않게
        if (ExcludeTarget != (ExcludeTarget | (1 << gameObject.layer)))
        {
            ExcludeTarget |= 1 << gameObject.layer;
        }
        _playerInput = GetComponent<PlayerInput>();
        _playerAnimator = GetComponentInChildren<Animator>();
        Gun = GetComponentInChildren<Gun>();
    }

    private void Start()
    {
        _playerCamera = Camera.main;
    }

    private void OnEnable()
    {
        AimState = EAimState.Idle;
        Gun.gameObject.SetActive(true);
        Gun.Setup(this);
    }

    private void OnDisable()
    {
        AimState = EAimState.Idle;
        Gun.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (_playerInput.fire)
        {
            Shoot();
        }
        else if (_playerInput.reload)
        {
            Reload();
        }
    }

    private void Update()
    {
        UpdateAimTarget();

        var angle = _playerCamera.transform.eulerAngles.x;
        if (angle > 270f) angle -= 360f;

        angle = angle / -180f + 0.5f;
        _playerAnimator.SetFloat("Angle", angle);

        if(!_playerInput.fire && Time.time >= _lastFireInputTime + _watingTimeForReleasingAim)
        {
            AimState = EAimState.Idle;
        }

        UpdateUI();
    }

    public void Shoot()
    {
        if(AimState == EAimState.Idle)
        {
            if (_isLinedUp) AimState = EAimState.HipFire;
        }
        else if(AimState == EAimState.HipFire)
        {
            if(_hasEnoughDistance)
            {
                if(Gun.Fire(_aimPoint))
                {
                    _playerAnimator.SetTrigger("Shoot");
                }
            }
            else
            {
                AimState = EAimState.Idle;
            }
        }
    }

    public void Reload()
    {
        if(Gun.Reload())
        {
            _playerAnimator.SetTrigger("Reload");
        }
    }

    private void UpdateAimTarget()
    {
        RaycastHit hit;

        var ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f,0.5f,0));

        if(Physics.Raycast(ray, out hit, Gun.FireDistance, ~ExcludeTarget))
        {
            _aimPoint = hit.point;
            // 카메라에서 발사한 포인트와 모델에서 발사한 포인트 비교 시 모델과 포인트 사이에 무언가 없는지 판별
            if(Physics.Linecast(Gun.FireTransform.position, hit.point, out hit, ~ExcludeTarget))
            {
                // 모델로부터의 실제 포인트
                _aimPoint = hit.point;
            }
        }
        else    // 레이캐스트에 아무것도 닿는게 없다면 최대 사정거리까지
        {
            _aimPoint = _playerCamera.transform.position + _playerCamera.transform.forward * Gun.FireDistance;
        }
    }

    private void UpdateUI()
    {
        if (Gun == null || UIManager.Instance == null) return;
        
        UIManager.Instance.UpdateAmmoText(Gun.MagAmmo, Gun.AmmoRemain);
        
        UIManager.Instance.SetActiveCrosshair(_hasEnoughDistance);
        UIManager.Instance.UpdateCrossHairPosition(_aimPoint);
    }

    private void OnAnimatorIK(int layerIndex)
    {

    }
}