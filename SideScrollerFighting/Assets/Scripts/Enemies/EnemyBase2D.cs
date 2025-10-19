// Assets/Scripts/Enemies/EnemyBase2D.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyBase2D : MonoBehaviour, IDamageable
{
    [Header("Core")]
    public int maxHP = 30;
    public float moveSpeed = 2.5f;
    public float gravityScale = 3f;

    [Header("Layers & Masks")]
    public LayerMask groundMask;   // Ground
    public LayerMask playerMask;   // Player (важно назначить!)

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundBox = new(0.6f, 0.1f);

    [Header("Animator (optional)")]
    public Animator animator;
    public string pSpeedX = "SpeedX";
    public string pGround = "IsGrounded";
    public string tHurt = "Hurt";
    public string tDie = "Die";

    protected Rigidbody2D rb;
    protected Transform player;
    protected int hp;
    protected bool grounded;
    protected bool facingRight = true;
    protected bool alive = true;

    public bool IsAlive => alive;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;

        hp = maxHP;

        // Поиск игрока по тегу
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged) player = tagged.transform;

        // Бэкап: если тега нет — ищем по слою в большом радиусе
        if (!player && playerMask.value != 0)
        {
            var c = Physics2D.OverlapCircle(transform.position, 100f, playerMask);
            if (c) player = c.transform;
        }
    }

    protected virtual void Update()
    {
        if (animator)
        {
            animator.SetFloat(pSpeedX, Mathf.Abs(rb.velocity.x));
            animator.SetBool(pGround, grounded);
        }
    }

    protected virtual void FixedUpdate()
    {
        grounded = groundCheck &&
                   Physics2D.OverlapBox(groundCheck.position, groundBox, 0f, groundMask);
    }

    protected void FaceTowards(Vector2 targetPos)
    {
        bool right = targetPos.x >= transform.position.x;
        if (right != facingRight)
        {
            facingRight = right;
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (facingRight ? 1 : -1);
            transform.localScale = s;
        }
    }

    public virtual void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        if (!alive) return;
        hp -= amount;
        if (animator) animator.SetTrigger(tHurt);

        if (hp <= 0)
        {
            alive = false;
            if (animator) animator.SetTrigger(tDie);
            Destroy(gameObject, 0.1f);
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(groundCheck.position, groundBox);
        }
    }
}
