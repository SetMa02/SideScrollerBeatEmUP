// Assets/Scripts/Player/PlayerCombat2D.cs
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerMotor2D))]
[DisallowMultipleComponent]
public class PlayerCombat2D : MonoBehaviour
{
    // ---------- ССЫЛКИ ----------
    [Header("Refs")]
    public Animator animator;
    private PlayerMotor2D _motor;
    private Rigidbody2D _rb;

    // ---------- АТАКА ----------
    [Header("Input")]
    public KeyCode attackKey = KeyCode.Mouse0;
    public KeyCode specialKey = KeyCode.Mouse1;

    [Header("Cooldowns")]
    public float attackCooldown = 0.35f;
    public float specialCooldown = 0.80f;

    [Header("Hit Window (если без Animation Events)")]
    public float hitboxEnableTime = 0.08f;
    public float hitboxDisableTime = 0.18f;
    public float spHitOnTime = 0.14f;
    public float spHitOffTime = 0.28f;

    [Header("Animator Triggers")]
    public string trigAttack = "Attack";
    public string trigSpecial = "Special";
    public string trigHurt = "Hurt";
    public string trigDie = "Die";

    [Header("Damage Out (по врагам)")]
    public LayerMask enemyMask;
    public Vector2 hitBoxSize = new(1.2f, 0.9f);
    public Vector2 hitBoxOffset = new(0.8f, 0.2f); // по X умножится на направление взгляда
    public int attackDamage = 15;
    public int specialDamage = 30;

    [Header("Anti-spam")]
    public bool hitOncePerSwing = true;  // один раз за взмах
    public float perTargetCooldown = 0.20f; // если hitOncePerSwing = false

    [Header("Debug")]
    public bool drawGizmos = true;

    // runtime атаки
    float _atkCd, _spCd;
    bool _isAttacking;
    bool _swingActive, _specialSwingActive;
    readonly HashSet<Object> _hitThisSwing = new();
    readonly Dictionary<Object, float> _nextHitTime = new();

    // ---------- ПОЛУЧЕНИЕ УРОНА / СМЕРТЬ ----------
    [Header("Player HP")]
    public int maxHP = 100;
    public int currentHP = 0; // если 0 — инициализируется maxHP в Awake

    [Header("Hurt Reaction")]
    public float hurtLockDuration = 0.25f;   // блок управления
    public float invulnDuration = 0.60f;   // i-frames
    public Vector2 knockback = new(6f, 7f);

    [Header("I-Frames: временно игнорируем слои")]
    public string playerLayerName = "Player";
    public string[] ignoreWithLayers = { "Enemy", "EnemyAttack", "Hazard", "Projectile" };

    [Header("Visual Flash (optional)")]
    public SpriteRenderer sprite;
    public Color flashColor = new(1f, 0.5f, 0.5f, 1f);
    public float flashInterval = 0.08f;

    // runtime урона
    bool _iframes = false;
    bool _alive = true;
    int _playerLayer;
    int[] _ignoredLayers;
    Color _origColor;

    // ========== UNITY ==========
    void Awake()
    {
        _motor = GetComponent<PlayerMotor2D>();
        _rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponent<Animator>();
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite) _origColor = sprite.color;

        if (currentHP <= 0) currentHP = maxHP;

        // подготовим слои для IgnoreLayerCollision
        _playerLayer = gameObject.layer;
        int pByName = LayerMask.NameToLayer(playerLayerName);
        if (pByName >= 0 && pByName <= 31) _playerLayer = pByName;

        _ignoredLayers = new int[ignoreWithLayers.Length];
        for (int i = 0; i < ignoreWithLayers.Length; i++)
            _ignoredLayers[i] = LayerMask.NameToLayer(ignoreWithLayers[i]);
    }

    void Start()
    {
        _swingActive = _specialSwingActive = false;
    }

    void Update()
    {
        if (!_alive) return;

        _atkCd -= Time.deltaTime;
        _spCd -= Time.deltaTime;

        if (Input.GetKeyDown(attackKey) && _atkCd <= 0f)
        {
            _atkCd = attackCooldown;
            StartAttack();
        }
        if (Input.GetKeyDown(specialKey) && _spCd <= 0f)
        {
            _spCd = specialCooldown;
            StartSpecial();
        }

        // наносим урон во время «окон»
        if (_swingActive) DoMeleeHit(attackDamage);
        if (_specialSwingActive) DoMeleeHit(specialDamage);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        float dirSign = Mathf.Sign(transform.localScale.x == 0 ? 1 : transform.localScale.x);
        Vector3 center = transform.position + new Vector3(hitBoxOffset.x * dirSign, hitBoxOffset.y, 0);
        Gizmos.color = (_swingActive || _specialSwingActive) ? Color.red : new Color(1, 0, 0, 0.35f);
        Gizmos.DrawWireCube(center, (Vector3)hitBoxSize);
    }

    // ========== АТАКА ==========
    void StartAttack()
    {
        if (animator) { animator.ResetTrigger(trigAttack); animator.SetTrigger(trigAttack); }
        _isAttacking = true;
        _motor.SetMovementLock(true);

        CancelInvoke(nameof(HB_On));
        CancelInvoke(nameof(HB_Off));
        Invoke(nameof(HB_On), hitboxEnableTime);
        Invoke(nameof(HB_Off), hitboxDisableTime);

        Invoke(nameof(EndAttack), attackCooldown * 0.95f);
    }

    void StartSpecial()
    {
        if (animator) { animator.ResetTrigger(trigSpecial); animator.SetTrigger(trigSpecial); }
        _isAttacking = true;
        _motor.SetMovementLock(true);

        CancelInvoke(nameof(SP_On));
        CancelInvoke(nameof(SP_Off));
        Invoke(nameof(SP_On), spHitOnTime);
        Invoke(nameof(SP_Off), spHitOffTime);

        Invoke(nameof(EndAttack), specialCooldown * 0.95f);
    }

    void HB_On() => BeginSwing(false);
    void HB_Off() => EndSwing(false);
    void SP_On() => BeginSwing(true);
    void SP_Off() => EndSwing(true);

    void EndAttack()
    {
        _isAttacking = false;
        _motor.SetMovementLock(false);
        _swingActive = _specialSwingActive = false;
        _hitThisSwing.Clear();
    }

    // Animation Events (если используешь их в клипах)
    public void AE_AttackStart() { _isAttacking = true; _motor.SetMovementLock(true); }
    public void AE_HitboxOn() { HB_On(); }
    public void AE_HitboxOff() { HB_Off(); }
    public void AE_AttackEnd() { EndAttack(); }

    public void AE_SpecialStart() { _isAttacking = true; _motor.SetMovementLock(true); }
    public void AE_SpecialOn() { SP_On(); }
    public void AE_SpecialOff() { SP_Off(); }
    public void AE_SpecialEnd() { EndAttack(); }

    void BeginSwing(bool special)
    {
        if (special) _specialSwingActive = true; else _swingActive = true;
        _hitThisSwing.Clear();
        // Debug.Log(special ? "[Combat] SPECIAL swing ON" : "[Combat] swing ON");
    }

    void EndSwing(bool special)
    {
        if (special) _specialSwingActive = false; else _swingActive = false;
        // Debug.Log(special ? "[Combat] SPECIAL swing OFF" : "[Combat] swing OFF");
    }

    void DoMeleeHit(int damage)
    {
        float dirSign = Mathf.Sign(transform.localScale.x == 0 ? 1 : transform.localScale.x);
        Vector2 center = (Vector2)transform.position + new Vector2(hitBoxOffset.x * dirSign, hitBoxOffset.y);

        var hits = Physics2D.OverlapBoxAll(center, hitBoxSize, 0f, enemyMask);
        foreach (var c in hits)
        {
            if (!c) continue;
            Object key = c.attachedRigidbody ? (Object)c.attachedRigidbody : c;

            if (hitOncePerSwing)
            {
                if (_hitThisSwing.Contains(key)) continue;
                _hitThisSwing.Add(key);
            }
            else
            {
                float now = Time.time;
                if (_nextHitTime.TryGetValue(key, out float t) && now < t) continue;
                _nextHitTime[key] = now + perTargetCooldown;
            }

            bool damaged = false;

            // 1) IDamageable — если есть
            if (c.TryGetComponent(out IDamageable dmg))
            {
                dmg.TakeDamage(damage, c.transform.position, Vector2.right * dirSign);
                damaged = true;
            }
            else
            {
                // 2) Явно ищем GetHit(float) или GetHit(int) у скрипта врага
                var comp = c.GetComponent<MonoBehaviour>();
                if (comp != null)
                {
                    var type = comp.GetType();
                    var mFloat = type.GetMethod("GetHit",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        binder: null, types: new[] { typeof(float) }, modifiers: null);
                    if (mFloat != null) { mFloat.Invoke(comp, new object[] { (float)damage }); damaged = true; }
                    else
                    {
                        var mInt = type.GetMethod("GetHit",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                            binder: null, types: new[] { typeof(int) }, modifiers: null);
                        if (mInt != null) { mInt.Invoke(comp, new object[] { damage }); damaged = true; }
                    }
                }

                if (!damaged)
                    c.gameObject.SendMessage("GetHit", (float)damage, SendMessageOptions.DontRequireReceiver);
            }

            Debug.Log($"[Combat] Hit '{c.name}' for {damage}");
        }
    }

    // ========== ПОЛУЧЕНИЕ УРОНА / СМЕРТЬ ==========
    /// <summary>Универсальный приём урона (враги могут звать его напрямую)</summary>
    public void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        if (!_alive) return;
        if (_iframes) return;

        int dmg = Mathf.Max(0, amount);
        currentHP = Mathf.Clamp(currentHP - dmg, 0, maxHP);
        Debug.Log($"[Player] Took {dmg}. HP = {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            Die();
            return;
        }

        // триггерим Hurt
        if (animator) animator.SetTrigger(trigHurt);

        // блок управления
        _motor.SetMovementLock(true);

        // откидывание
        Vector2 dir = hitNormal;
        if (dir.sqrMagnitude < 1e-3f)
            dir = (transform.position.x >= hitPoint.x) ? Vector2.right : Vector2.left;

        var v = _rb.velocity;
        if (v.y < 0f) v.y = 0f;
        Vector2 kb = new(Mathf.Sign(dir.x) * knockback.x, Mathf.Max(v.y, knockback.y));
        _rb.velocity = kb;

        StopAllCoroutines();
        StartCoroutine(HurtRoutine());
    }

    /// <summary>Сахар для вызовов GetHit(float/int) со стороны врагов.</summary>
    public void GetHit(float damage) => TakeDamage(Mathf.RoundToInt(Mathf.Abs(damage)), transform.position, Vector2.zero);
    public void GetHit(int damage) => TakeDamage(Mathf.Abs(damage), transform.position, Vector2.zero);

    void Die()
    {
        if (!_alive) return;
        _alive = false;

        Debug.Log("[Player] DIED");

        if (animator) animator.SetTrigger(trigDie);
        _motor.SetMovementLock(true);

        // можно отключить боёвку, чтобы не атаковал после смерти
        this.enabled = true; // оставляем скрипт включённым ради анима-событий; логику ввода уже блокирует _alive=false

        // при необходимости: отключить коллайдеры/управление снаружи
        // foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
    }

    IEnumerator HurtRoutine()
    {
        _iframes = true;
        ToggleIgnore(true);

        float lockTimer = hurtLockDuration;
        float t = 0f;
        float blink = 0f;

        while (t < invulnDuration)
        {
            // мерцание
            if (sprite)
            {
                blink -= Time.deltaTime;
                if (blink <= 0f)
                {
                    sprite.color = (sprite.color == _origColor) ? flashColor : _origColor;
                    blink = flashInterval;
                }
            }

            // снимем лок через hurtLockDuration
            lockTimer -= Time.deltaTime;
            if (lockTimer <= 0f && _motor.MovementLocked)
                _motor.SetMovementLock(false);

            t += Time.deltaTime;
            yield return null;
        }

        if (sprite) sprite.color = _origColor;
        ToggleIgnore(false);
        _iframes = false;

        if (_alive && _motor.MovementLocked)
            _motor.SetMovementLock(false);
    }

    void ToggleIgnore(bool on)
    {
        if (_playerLayer < 0 || _playerLayer > 31) return;
        for (int i = 0; i < _ignoredLayers.Length; i++)
        {
            int lay = _ignoredLayers[i];
            if (lay < 0 || lay > 31) continue;
            Physics2D.IgnoreLayerCollision(_playerLayer, lay, on);
        }
    }
}

// Простой интерфейс; если у тебя уже есть свой — можешь удалить это объявление.