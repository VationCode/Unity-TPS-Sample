using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController m_characterController;
    private PlayerInput m_playerInput;
    private PlayerShooter m_playerShooter;
    private Animator m_animator;

    [SerializeField]
    private Camera m_followCam;
    
    public float Speed = 6f;
    public float JumpVelocity = 8f;
    [Range(0.01f, 1f)] public float AirControlPercent;

    public float SpeedSmoothTime = 0.1f;
    public float TurnSmoothTime = 0.1f;
    
    private float m_speedSmoothVelocity;
    private float m_turnSmoothVelocity;
    
    private float m_currentVelocityY;

    [SerializeField]
    private float m_gravity = -9.8f;
    // m_characterController 실제 움직임이고 있는 값을 기반으로한 속력값
    public float CurrentSpeed =>
        new Vector2(m_characterController.velocity.x, m_characterController.velocity.z).magnitude;

    private bool _isJump;
    private void Awake()
    {
        m_characterController = GetComponent<CharacterController>();
        m_playerInput = GetComponent<PlayerInput>();
        m_animator = GetComponentInChildren<Animator>();
        m_playerShooter = GetComponent<PlayerShooter>();
        m_followCam = Camera.main;
    }

    private void FixedUpdate()
    {
        if (CurrentSpeed > 0.2f || m_playerInput.fire || m_playerShooter.AimState == PlayerShooter.EAimState.HipFire)
            Rotate();

        Move(m_playerInput.moveInput);

    }

    private void Update()
    {
        if (m_playerInput.jump) Jump();
        UpdateAnimation(m_playerInput.moveInput);
    }

    public void Move(Vector2 p_moveInput)
    {
        // 속력 및 방향
        var targetSpeed = Speed * p_moveInput.magnitude;
        var moveDiection = Vector3.Normalize(transform.forward * p_moveInput.y + transform.right * p_moveInput.x);

        // 스피드 부드럽게 전환 , 공중에서는 움직임 값 변화 거의 없게
        var smoothTime = m_characterController.isGrounded ? SpeedSmoothTime : SpeedSmoothTime / AirControlPercent;

        targetSpeed = Mathf.SmoothDamp(CurrentSpeed, targetSpeed, ref m_speedSmoothVelocity, smoothTime);

        // 속도Y 즉, 중력값 
        m_currentVelocityY += Time.deltaTime * m_gravity;
        var velocity = moveDiection * targetSpeed + Vector3.up * m_currentVelocityY;

        m_characterController.Move(velocity * Time.deltaTime);

        if (m_characterController.isGrounded) m_currentVelocityY = 0f;
    }

    public void Rotate()
    {
        var targetRot = m_followCam.transform.eulerAngles.y;

        targetRot = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRot,ref m_turnSmoothVelocity, TurnSmoothTime);

        transform.eulerAngles = Vector3.up * targetRot;
    }

    public void Jump()
    {
        if (!m_characterController.isGrounded) return;
        m_currentVelocityY = JumpVelocity;
    }

    private void UpdateAnimation(Vector2 p_moveInput)
    {
        // CurrentSpeed는 CharacterController 기반이기에 즉, 캐릭터의 움직임 기반
        // 벽에 부딪혔을 시 CurrentSpeed는 0이 되기에 그러한 값을 반영하여 벽쪽으로 움직임은 멈춘 애니메이션 반영
        var animSpeedPer = CurrentSpeed / Speed;
        m_animator.SetFloat("Vertical Move", p_moveInput.y * animSpeedPer, 0.05f, Time.deltaTime);
        m_animator.SetFloat("Horizontal Move", p_moveInput.x * animSpeedPer, 0.05f, Time.deltaTime);
    }
}