// Assets/Scripts/Player/PlayerMotor2D.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor2D : MonoBehaviour
{
    [Header("Move")]
    public float maxWalkSpeed = 4.5f;
    public float maxRunSpeed = 7.5f;
    public float accel = 40f;
    public float decel = 50f;
    public float velPower = 0.9f; // сглаживание ускорения

    [Header("Jump")]
    public float jumpForce = 12f;
    public float coyoteTime = 0.12f;
    public float jumpBuffer = 0.12f;
    [Tooltip("Сколько раз быстрее обрезать вертикальную скорость при отпускании прыжка")]
    public float jumpCutMultiplier = 2.5f;

    [Header("Gravity")]
    public float gravityScale = 3f;
    public float fallGravityMultiplier = 1.2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundBoxSize = new(0.8f, 0.15f);
    public LayerMask groundMask;

    [Header("Control")]
    public KeyCode runKey = KeyCode.LeftShift;

    // Runtime
    Rigidbody2D _rb;
    bool _isGrounded;
    bool _facingRight = true;

    float _lastGroundedTime; // для coyote
    float _lastJumpPressedTime; // для jump buffer

    public bool IsGrounded => _isGrounded;
    public bool IsRunning { get; private set; }
    public float InputX { get; private set; }
    public Vector2 Velocity => _rb.velocity;
    public bool FacingRight => _facingRight;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = gravityScale;
    }

    void Update()
    {
        // INPUT
        InputX = Input.GetAxisRaw("Horizontal");
        IsRunning = Input.GetKey(runKey);

        // буфер прыжка
        if (Input.GetButtonDown("Jump"))
            _lastJumpPressedTime = jumpBuffer;

        // отпускание прыжка для «короткого» прыжка
        if (Input.GetButtonUp("Jump") && _rb.velocity.y > 0)
            _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y / jumpCutMultiplier);

        // флип
        if (InputX != 0)
        {
            bool right = InputX > 0;
            if (right != _facingRight)
            {
                _facingRight = right;
                var s = transform.localScale;
                s.x = Mathf.Abs(s.x) * (right ? 1 : -1);
                transform.localScale = s;
            }
        }

        // таймеры
        _lastGroundedTime -= Time.deltaTime;
        _lastJumpPressedTime -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        GroundCheck();

        // Прыжок: coyote + buffer
        if (_lastJumpPressedTime > 0 && _lastGroundedTime > 0)
        {
            _lastJumpPressedTime = 0;
            _lastGroundedTime = 0;
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
        }

        // Движение по X с плавным ускорением/торможением
        float targetSpeed = (IsRunning ? maxRunSpeed : maxWalkSpeed) * InputX;
        float speedDiff = targetSpeed - _rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accel : decel;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velPower) * Mathf.Sign(speedDiff);

        _rb.AddForce(movement * Vector2.right);

        // Гравитация усиленная при падении
        if (_rb.velocity.y < -0.01f)
            _rb.gravityScale = gravityScale * fallGravityMultiplier;
        else
            _rb.gravityScale = gravityScale;
    }

    void GroundCheck()
    {
        if (!groundCheck) { _isGrounded = false; return; }
        _isGrounded = Physics2D.OverlapBox(groundCheck.position, groundBoxSize, 0f, groundMask);

        if (_isGrounded)
            _lastGroundedTime = coyoteTime;
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
