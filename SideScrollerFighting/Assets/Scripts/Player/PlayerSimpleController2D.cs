// Assets/Scripts/Player/PlayerSimpleController2D.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerSimpleController2D : MonoBehaviour, IDamageable
{
    [Header("Move")]
    public float moveSpeed = 6f;
    public float crouchSpeedMultiplier = 0.5f;
    public float jumpForce = 9f;
    public float gravityScale = 3f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    public LayerMask groundMask = 1 << 6;  // Layer "Ground"

    [Header("Crouch")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public Vector2 standingColliderSize = new Vector2(0.9f, 1.8f);
    public Vector2 standingColliderOffset = new Vector2(0f, -0.1f);
    public Vector2 crouchColliderSize = new Vector2(0.9f, 1.2f);
    public Vector2 crouchColliderOffset = new Vector2(0f, -0.4f);

    [Header("Attack")]
    public Transform attackPoint;
    public float attackRadius = 1.1f;
    public LayerMask enemyMask = 1 << 7; // Layer "Enemy"
    public float attackCooldown = 0.25f;

    [Header("Debug/Anim Hooks")]
    public Animator animator; // опционально
    public string animParamSpeed = "Speed";
    public string animParamGrounded = "IsGrounded";
    public string animParamCrouch = "IsCrouching";
    public string animTriggerAttack = "Attack";

    Rigidbody2D _rb;
    Collider2D _col;
    bool _grounded;
    bool _crouching;
    bool _facingRight = true;
    float _atkCd;
    bool _alive = true;

    public bool IsAlive => _alive;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _rb.gravityScale = gravityScale;
        gameObject.tag = "Player";
    }

    void Update()
    {
        if (!_alive) return;

        float h = Input.GetAxisRaw("Horizontal");
        bool wantJump = Input.GetButtonDown("Jump");
        bool holdCrouch = Input.GetKey(crouchKey);

        // Crouch
        SetCrouch(holdCrouch && _grounded);

        // Move
        float speed = moveSpeed * (_crouching ? crouchSpeedMultiplier : 1f);
        _rb.velocity = new Vector2(h * speed, _rb.velocity.y);

        // Flip
        if (h != 0)
        {
            bool right = h > 0;
            if (right != _facingRight)
            {
                _facingRight = right;
                Vector3 s = transform.localScale;
                s.x = Mathf.Abs(s.x) * (_facingRight ? 1 : -1);
                transform.localScale = s;
            }
        }

        // Jump
        if (wantJump && _grounded && !_crouching)
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);

        // Attack
        _atkCd -= Time.deltaTime;
        if (Input.GetMouseButtonDown(0) && _atkCd <= 0f)
        {
            DoAttack();
            _atkCd = attackCooldown;
        }

        // Animator hooks
        if (animator)
        {
            animator.SetFloat(animParamSpeed, Mathf.Abs(_rb.velocity.x));
            animator.SetBool(animParamGrounded, _grounded);
            animator.SetBool(animParamCrouch, _crouching);
        }
    }

    void FixedUpdate()
    {
        // Ground check (коробка под ногами)
        if (groundCheck)
            _grounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundMask);
        else
            _grounded = false;
    }

    void SetCrouch(bool state)
    {
        if (_crouching == state) return;
        _crouching = state;

        // меняем коллайдер под стойку/присяд
        if (_col is CapsuleCollider2D cap)
        {
            cap.size = _crouching ? crouchColliderSize : standingColliderSize;
            cap.offset = _crouching ? crouchColliderOffset : standingColliderOffset;
        }
        else if (_col is BoxCollider2D box)
        {
            box.size = _crouching ? crouchColliderSize : standingColliderSize;
            box.offset = _crouching ? crouchColliderOffset : standingColliderOffset;
        }
    }

    void DoAttack()
    {
        if (animator) animator.SetTrigger(animTriggerAttack);

        if (!attackPoint) return;
        var hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyMask);
        foreach (var h in hits)
        {
            if (h.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(9999, h.transform.position, Vector2.up); // “любой удар = смерть”
        }
    }

    // IDamageable (на будущее; сейчас игрок бессмертен)
    public void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        // Здесь можно повесить анимацию урона/смерти
        // _alive = false;  // если нужно, сделай смертность
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
        if (attackPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}
