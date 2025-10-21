using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public class EnemyBase2D : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int _maxHealth = 50;
    [SerializeField] private int _health = 50;
    [SerializeField] private float _speed = 2.5f;
    [SerializeField] private float _force = 6f;

    [Header("Animator Params")]
    private string Speed = "Speed";
    private string Attack = "Attack";
    private string IsDead = "IsDead"; // bool
    private string Hit = "Hit";       // trigger

    private Vector3 _direction;
    private bool _canMove = true;
    private bool _alive = true;

    private Rigidbody2D _rigidbody2D;
    private Animator _animator;
    private GameObject _player;
    private EnemyMeleeAI _enemyMeleeAI;
    private PlayerDetector _playerDetector;
    private Collider2D[] _allColliders;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _player = GameObject.FindGameObjectWithTag("Player");
        _enemyMeleeAI = GetComponentInChildren<EnemyMeleeAI>();
        _playerDetector = GetComponentInChildren<PlayerDetector>();
        _allColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);

        if (!_player) throw new Exception("Player not found");

        // инициализируем ХП
        if (_maxHealth <= 0) _maxHealth = 1;
        _health = Mathf.Clamp(_health <= 0 ? _maxHealth : _health, 0, _maxHealth);
        _alive = _health > 0;
    }

    private void OnEnable()
    {
        if (_playerDetector) _playerDetector.attackEvent.AddListener(AttackPlayer);
    }

    private void OnDisable()
    {
        if (_playerDetector) _playerDetector.attackEvent.RemoveListener(AttackPlayer);
    }

    private void FixedUpdate()
    {
        if (_alive) WalkToPlayer();
        else _rigidbody2D.velocity = new Vector2(0, _rigidbody2D.velocity.y);
    }

    private void AttackPlayer()
    {
        if (!_alive) return;
        _animator.SetTrigger(Attack);
    }

    // Вызывай этот метод из анимации (Animation Event) для включения/выключения коллайдера оружия
    private void ToggleAttackCollider()
    {
        if (!_enemyMeleeAI) return;
        var weaponCollider = _enemyMeleeAI.gameObject.GetComponent<BoxCollider2D>();
        if (weaponCollider) weaponCollider.enabled = !weaponCollider.enabled;
    }

    public void FreezeMovement()
    {
        _canMove = false;
        _rigidbody2D.velocity = Vector2.zero;
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
    }

    public void UnfreezeMovement()
    {
        _canMove = true;
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
    }

    private void WalkToPlayer()
    {
        if (!_canMove) return;

        _direction = Vector3.zero;
        float delta = _player.transform.position.x - transform.position.x;

        if (Mathf.Abs(delta) > 0.1f)
        {
            if (delta < 0)
            {
                _direction = Vector3.left;
                var scale = transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
            else
            {
                _direction = Vector3.right;
                var scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        _direction *= _speed;
        _direction.y = _rigidbody2D.velocity.y;
        _rigidbody2D.velocity = _direction;
        _animator.SetFloat(Speed, Mathf.Abs(_direction.x));
    }


    
    public void GetHit(float damage)
    {
        ApplyDamage(Mathf.RoundToInt(Mathf.Abs(damage)), default, default);
    }

    public void GetHit(int damage)
    {
        ApplyDamage(Mathf.Abs(damage), default, default);
    }

 
    public void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        ApplyDamage(Mathf.Abs(amount), hitPoint, hitNormal);
    }

    private void ApplyDamage(int amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        if (!_alive) return;

        _health = Mathf.Max(0, _health - amount);
        Debug.Log($"[Enemy] {name} took {amount} damage. HP = {_health}/{_maxHealth}");

        // триггерим «Hurt»
        _animator.SetTrigger(Hit);

        if (_health <= 0)
        {
            Die();
        }
        else
        {
            // лёгкий отскок/стоп — по желанию:
            _rigidbody2D.velocity = new Vector2(0f, _rigidbody2D.velocity.y);
        }
    }

    private void Die()
    {
        if (!_alive) return;
        _alive = false;

        // флаги анимации смерти
        _animator.SetBool(IsDead, true);

        // отключим возможность двигаться/атаковать
        FreezeMovement();
        foreach (var col in _allColliders)
            col.enabled = false; // если нужно, оставь базовый коллайдер для падения трупа

        Debug.Log($"[Enemy] {name} DIED");

        // по желанию: задержка на проигрывание анимации/смерти
        Destroy(gameObject, 1.0f);
    }
}
