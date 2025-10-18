// Assets/Scripts/Player/AttackHitbox.cs
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public LayerMask targetMask;      // Enemy
    public int damage = 20;
    public Vector2 boxSize = new(1.2f, 0.9f);
    public Vector2 boxOffset = new(0.8f, 0.2f);
    public bool active;

    Transform _owner;

    void Awake() { _owner = transform.root; }

    void Update()
    {
        if (!active) return;

        // ориентируем оффсет по направлению владельца
        float dir = Mathf.Sign(_owner.localScale.x);
        Vector2 center = (Vector2)transform.position + new Vector2(boxOffset.x * dir, boxOffset.y);

        var hits = Physics2D.OverlapBoxAll(center, boxSize, 0f, targetMask);
        foreach (var c in hits)
        {
            if (c.attachedRigidbody && c.attachedRigidbody.transform == _owner) continue;

            if (TryGetDamageable(c, out var d) && d.IsAlive)
            {
                d.TakeDamage(damage, c.transform.position, Vector2.up);
            }
        }
    }

    public void SetActive(bool on) => active = on;

    bool TryGetDamageable(Component c, out IDamageable d)
    {
        d = c.GetComponent<IDamageable>();
        if (d != null) return true;
        d = c.GetComponentInParent<IDamageable>();
        if (d != null) return true;
        d = c.GetComponentInChildren<IDamageable>();
        return d != null;
    }

    void OnDrawGizmosSelected()
    {
        if (!_owner) _owner = transform.root;
        Gizmos.color = active ? Color.red : new Color(1, 0, 0, 0.3f);
        float dir = _owner ? Mathf.Sign(_owner.localScale.x) : 1f;
        Vector3 center = transform.position + new Vector3(boxOffset.x * dir, boxOffset.y, 0);
        Gizmos.DrawWireCube(center, (Vector3)boxSize);
    }
}
