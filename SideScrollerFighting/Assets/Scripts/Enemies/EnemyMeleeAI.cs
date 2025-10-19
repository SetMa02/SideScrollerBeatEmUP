// Assets/Scripts/Enemies/EnemyMeleeAI.cs
using UnityEngine;

public class EnemyMeleeAI : EnemyBase2D
{
    [Header("Patrol")]
    public float patrolSpeed = 2f;
    [Tooltip("Смещение точки проверки края вперёд, по направлению движения")]
    public Vector2 ledgeCheckOffset = new(0.5f, 0f);
    [Tooltip("Радиус проверки земли перед ногой (стабильнее, чем Raycast)")]
    public float ledgeCheckRadius = 0.18f;
    public LayerMask ledgeMask; // обычно тот же groundMask
    [Tooltip("КД на поворот, чтобы не дёргаться на границе тайлов")]
    public float turnCooldown = 0.35f;

    [Header("Detect/Chase")]
    public float visionRange = 6f;
    public float chaseSpeed = 3.2f;
    public float loseRange = 8f;
    [Tooltip("Не флипаться, если игрок почти по одной вертикали (мёртвая зона X)")]
    public float faceDeadZoneX = 0.2f;
    [Tooltip("Нужна ли прямая видимость (Line of Sight) до игрока")]
    public bool requireLineOfSight = true;

    [Header("Attack")]
    public float attackRange = 1.2f;
    public float attackCooldown = 0.8f;
    public int damage = 10;

    [Header("Attack Hitbox")]
    public Vector2 boxSize = new(1.4f, 1.0f);
    public Vector2 boxOffset = new(0.9f, 0.0f);

    float atkCd;
    int dir = -1;          // -1=влево, 1=вправо
    float turnCdTimer;

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!alive)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        if (turnCdTimer > 0f) turnCdTimer -= Time.fixedDeltaTime;
        atkCd -= Time.fixedDeltaTime;

        // Детект игрока по маске (в радиусе)
        Collider2D seen = Physics2D.OverlapCircle(transform.position, visionRange, playerMask);
        if (seen) player = seen.transform; // обновим ссылку, если появилась

        bool seePlayer = player && (!requireLineOfSight || CanSeePlayer());
        bool inAttack = player && Vector2.Distance(player.position, transform.position) <= attackRange + 0.05f;

        if (seePlayer)
        {
            SafeFaceTowards(player.position); // с мёртвой зоной
            dir = facingRight ? 1 : -1;

            if (inAttack && atkCd <= 0f)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                DoAttack();
                atkCd = attackCooldown;
            }
            else
            {
                rb.velocity = new Vector2(chaseSpeed * dir, rb.velocity.y);
            }
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        // База точки для проверки (у земли)
        Vector2 basePos = groundCheck ? (Vector2)groundCheck.position : (Vector2)transform.position;
        Vector2 forward = new Vector2(dir * ledgeCheckOffset.x, ledgeCheckOffset.y);
        Vector2 probe = basePos + forward;

        bool groundAhead = Physics2D.OverlapCircle(probe, ledgeCheckRadius, ledgeMask);
        bool wallAhead = Physics2D.Raycast(basePos, new Vector2(dir, 0), 0.35f, groundMask);

        if ((!groundAhead || wallAhead) && turnCdTimer <= 0f)
        {
            dir *= -1;
            turnCdTimer = turnCooldown;
        }

        SafeFaceTowards((Vector2)transform.position + new Vector2(dir, 0));
        rb.velocity = new Vector2(patrolSpeed * dir, rb.velocity.y);
    }

    void DoAttack()
    {
        if (animator) animator.SetTrigger("Attack");
        float sign = facingRight ? 1f : -1f;
        Vector2 center = (Vector2)transform.position + new Vector2(boxOffset.x * sign, boxOffset.y);
        var hits = Physics2D.OverlapBoxAll(center, boxSize, 0f, playerMask);
        foreach (var c in hits)
        {
            if (c.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(damage, c.transform.position, Vector2.right * sign);
        }
    }

    bool CanSeePlayer()
    {
        if (!player) return false;
        // небольшой подъём луча, чтобы не цеплять пол
        Vector2 from = (Vector2)transform.position + Vector2.up * 0.2f;
        Vector2 to = (Vector2)player.position + Vector2.up * 0.2f;

        // Проверяем только столкновения с землёй/стенами (groundMask)
        var hit = Physics2D.Linecast(from, to, groundMask);
        Debug.DrawLine(from, to, hit ? Color.yellow : Color.green, 0.05f);
        return !hit;
    }

    void SafeFaceTowards(Vector2 targetPos)
    {
        float dx = targetPos.x - transform.position.x;
        if (Mathf.Abs(dx) < faceDeadZoneX) return; // мёртвая зона — не дёргаемся
        bool right = dx >= 0f;
        if (right != facingRight)
        {
            facingRight = right;
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (facingRight ? 1 : -1);
            transform.localScale = s;
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = new Color(0, 1, 1, 0.25f);
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        float sign = facingRight ? 1f : -1f;
        Vector3 center = transform.position + new Vector3(boxOffset.x * sign, boxOffset.y, 0);
        Gizmos.DrawWireCube(center, (Vector3)boxSize);

        Gizmos.color = Color.magenta;
        Vector2 basePos = groundCheck ? (Vector2)groundCheck.position : (Vector2)transform.position;
        Vector2 forward = new Vector2(dir * ledgeCheckOffset.x, ledgeCheckOffset.y);
        Vector2 probe = basePos + forward;
        Gizmos.DrawWireSphere(probe, ledgeCheckRadius);
        Gizmos.DrawLine(basePos, basePos + new Vector2(dir * 0.35f, 0));
    }
}
