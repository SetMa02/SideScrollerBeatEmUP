// Assets/Scripts/Player/PlayerMotor2D.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor2D : MonoBehaviour
{
    [Header("Move")]
    public float maxWalkSpeed = 4.5f;
    public float maxRunSpeed = 7.5f;
    [Tooltip("�������� ������/������ �������� (��� ������ � ��� ������)")]
    public float accel = 40f;
    public float decel = 50f;
    [Tooltip("����������� ���������: 0.7�1.0 (������ � �����)")]
    [Range(0.7f, 1.2f)] public float velPower = 0.9f;

    [Header("Jump")]
    public float jumpForce = 12f;
    [Tooltip("���� ����� ����� � ���������, � ������� ��� ����� ��������")]
    public float coyoteTime = 0.12f;
    [Tooltip("����, � ������� ����� ������ ������ �� ������� �����")]
    public float jumpBuffer = 0.12f;
    [Tooltip("�� ������� ��� �������� ������������ �������� ��� ���������� ������")]
    public float jumpCutMultiplier = 2.5f;

    [Header("Gravity")]
    public float gravityScale = 3f;
    [Tooltip("��������� ���������� ��� �������")]
    public float fallGravityMultiplier = 1.2f;
    [Tooltip("����������� ������������ �������� �������")]
    public float terminalVelocity = -25f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundBoxSize = new(0.8f, 0.15f);
    public LayerMask groundMask;

    [Header("Control")]
    public KeyCode runKey = KeyCode.LeftShift;

    // --- Runtime state ---
    Rigidbody2D _rb;
    bool _isGrounded;
    bool _facingRight = true;

    float _lastGroundedTime;       // ������ ��� coyote
    float _lastJumpPressedTime;    // ������ ��� jump buffer

    // ��� ��������/������� �������� (��������, ������)
    public bool MovementLocked { get; private set; }
    float _externalVelX;
    float _externalVelXTimer;

    // --- Public read-only API ---
    public bool IsGrounded => _isGrounded;
    public bool IsRunning { get; private set; }
    public float InputX { get; private set; }
    public Vector2 Velocity => _rb ? _rb.velocity : Vector2.zero;
    public bool FacingRight => _facingRight;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = gravityScale;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        // ���� ����������� ��� MovementLocked
        if (!MovementLocked)
        {
            InputX = Input.GetAxisRaw("Horizontal");
            IsRunning = Input.GetKey(runKey);
        }
        else
        {
            InputX = 0f;
            IsRunning = false;
        }

        // Jump buffer
        if (Input.GetButtonDown("Jump"))
            _lastJumpPressedTime = jumpBuffer;

        // Jump cut (�������� ������)
        if (Input.GetButtonUp("Jump") && _rb.velocity.y > 0f)
            _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y / jumpCutMultiplier);

        // ���� �� ����������� �����
        if (InputX != 0)
        {
            bool wantRight = InputX > 0;
            if (wantRight != _facingRight)
            {
                _facingRight = wantRight;
                var s = transform.localScale;
                s.x = Mathf.Abs(s.x) * (_facingRight ? 1 : -1);
                transform.localScale = s;
            }
        }

        // �������
        _lastGroundedTime -= Time.deltaTime;
        _lastJumpPressedTime -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        GroundCheck();

        // ������: �����������, ���� � ���� coyote � ��� ����� �������
        if (_lastJumpPressedTime > 0f && _lastGroundedTime > 0f)
        {
            _lastJumpPressedTime = 0f;
            _lastGroundedTime = 0f;
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
        }

        // ��������� ���������� ��� �������
        if (_rb.velocity.y < -0.01f)
            _rb.gravityScale = gravityScale * fallGravityMultiplier;
        else
            _rb.gravityScale = gravityScale;

        // ��������� ������������ �������� �������
        if (_rb.velocity.y < terminalVelocity)
            _rb.velocity = new Vector2(_rb.velocity.x, terminalVelocity);

        // ������� ������� �� X (��������, ������) ����� ���������
        if (_externalVelXTimer > 0f)
        {
            _rb.velocity = new Vector2(_externalVelX, _rb.velocity.y);
            _externalVelXTimer -= Time.fixedDeltaTime;
            return;
        }

        // �� ����� ���� � ������ X = 0
        if (MovementLocked)
        {
            _rb.velocity = new Vector2(0f, _rb.velocity.y);
            return;
        }

        // ������� �������� �� X � ������� ����������/�����������
        float targetSpeed = (IsRunning ? maxRunSpeed : maxWalkSpeed) * InputX;
        float speedDiff = targetSpeed - _rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accel : decel;

        // ������������ ���� � �������� ���������, ��� �������� AddForce
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velPower) * Mathf.Sign(speedDiff);
        _rb.AddForce(movement * Vector2.right);

        if (_isGrounded && Mathf.Abs(InputX) < 0.01f && Mathf.Abs(_rb.velocity.x) < 0.2f)
        {
            // �������: ���� ����� ����� � ��� ����� � �������, ��� �����
            _rb.velocity = new Vector2(0f, _rb.velocity.y);
        }
    }

    void GroundCheck()
    {
        if (!groundCheck)
        {
            _isGrounded = false;
            return;
        }

        _isGrounded = Physics2D.OverlapBox(groundCheck.position, groundBoxSize, 0f, groundMask);
        if (_isGrounded)
            _lastGroundedTime = coyoteTime;
    }

    // --- Public helpers ---

    /// <summary>��������� ��������� �������������� �������� �� ������ false.</summary>
    public void SetMovementLock(bool locked)
    {
        MovementLocked = locked;
        if (locked)
            _rb.velocity = new Vector2(0f, _rb.velocity.y);
    }

    /// <summary>����� ������� �������������� �������� �� duration ������ (��� �������/�����).</summary>
    public void ApplyExternalVelocityX(float velX, float duration)
    {
        _externalVelX = velX;
        _externalVelXTimer = Mathf.Max(0f, duration);
    }

    /// <summary>������������� ��������� ���������.</summary>
    public void FaceDirection(bool faceRight)
    {
        if (_facingRight == faceRight) return;
        _facingRight = faceRight;
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (_facingRight ? 1 : -1);
        transform.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(groundCheck.position, groundBoxSize);
        }
    }
}
