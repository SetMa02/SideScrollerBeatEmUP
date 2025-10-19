// Assets/Scripts/Player/AttackHitbox.cs
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Targeting")]
    public LayerMask targetMask;                 // слой Enemy
    public int damage = 20;

    [Header("Shape")]
    public Vector2 boxSize = new(1.2f, 0.9f);
    public Vector2 boxOffset = new(0.8f, 0.2f);

    [Header("Anti-spam")]
    [Tooltip("Наносить урон каждому объекту максимум один раз за одно включение хитбокса")]
    public bool hitOncePerSwing = true;
    [Tooltip("Если false, используем КД на цель, чтобы не тикало каждый кадр")]
    public float perTargetCooldown = 0.2f;

    bool _active;
    Transform _owner;

    // Кого уже били в этом «взмахе»
    readonly HashSet<IDamageable> _hitThisSwing = new();
    // Время, когда можно снова бить цель (если hitOncePerSwing = false)
    readonly Dictionary<IDamageable, float> _nextHitTime = new();

    void Awake()
    {
        _owner = transform.root;
        _active = false; // по умолчанию выключен
    }

    void Update()
    {
        if (!_active) return;

        float dir = Mathf.Sign(_owner.localScale.x);
        Vector2 center = (Vector2)transform.position + new Vector2(boxOffset.x * dir, boxOffset.y);

        var hits = Physics2D.OverlapBoxAll(center, boxSize, 0f, targetMask);
        foreach (var c in hits)
        {
            if (!TryGetDamageable(c, out var dmg) || !dmg.IsAlive) continue;

            // не бьем владельца
            if (c.attachedRigidbody && c.attachedRigidbody.transform == _owner) continue;

            // анти-спам: один раз за свинг
            if (hitOncePerSwing)
            {
                if (_hitThisSwing.Contains(dmg)) continue;   // уже били в этот свинг
                dmg.TakeDamage(damage, c.transform.position, Vector2.up);
                _hitThisSwing.Add(dmg);
            }
            else
            {
                // пер-цельный КД
                float now = Time.time;
                if (_nextHitTime.TryGetValue(dmg, out float t) && now < t) continue;
                dmg.TakeDamage(damage, c.transform.position, Vector2.up);
                _nextHitTime[dmg] = now + perTargetCooldown;
            }
        }
    }

    public void SetActive(bool on)
    {
        if (on == _active) return;
        _active = on;

        if (_active)
        {
            // новый «взмах» — очистим список уже поражённых
            _hitThisSwing.Clear();
        }
    }

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
        Gizmos.color = _active ? Color.red : new Color(1, 0, 0, 0.35f);
        float dir = _owner ? Mathf.Sign(_owner.localScale.x) : 1f;
        Vector3 center = transform.position + new Vector3(boxOffset.x * dir, boxOffset.y, 0);
        Gizmos.DrawWireCube(center, (Vector3)boxSize);
    }
}
