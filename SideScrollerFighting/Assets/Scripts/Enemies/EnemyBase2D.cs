using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public class EnemyBase2D : MonoBehaviour
{
    [SerializeField]private float _health;
    [SerializeField]private float _speed;
    [SerializeField]private float _force;
    private string Speed = "Speed";
    private string Attack = "Attack";
    private string IsDead = "IsDead";
    private string Hit = "Hit";
    private Vector3 _direction;
    private bool _canMove = true;
    
    private Rigidbody2D _rigidbody2D;
    private Animator _animator;
    private GameObject _player;
    private EnemyMeleeAI _enemyMeleeAI;
    private PlayerDetector _playerDetector;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _player = GameObject.FindGameObjectWithTag("Player");
        _enemyMeleeAI = GetComponentInChildren<EnemyMeleeAI>();
        _playerDetector = GetComponentInChildren<PlayerDetector>();

        if (!_player)
        {
            throw new Exception("Player not found");
        }
    }

    private void OnEnable()
    {
        _playerDetector.attackEvent.AddListener(AttackPlayer);
    }

    private void OnDisable()
    {
        _playerDetector.attackEvent.RemoveListener(AttackPlayer);
    }

    private void FixedUpdate()
    {
        WalkToPlayer();
    }

    private void AttackPlayer()
    {
        _animator.SetTrigger(Attack);
    }

    private void ToggleAttackCollider()
    {
        BoxCollider2D weaponCollider = _enemyMeleeAI.gameObject.GetComponent<BoxCollider2D>();
        weaponCollider.enabled = !weaponCollider.enabled;
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
        if (_canMove)
        {
            _direction = Vector3.zero;
            float delta = _player.transform.position.x - transform.position.x;

            if (Mathf.Abs(delta) > 0.1f)
            {
                if (delta < 0)
                {
                    _direction = Vector3.left;
                    Vector3 scale = transform.localScale;
                    scale.x = -Mathf.Abs(scale.x) * 1f;
                    transform.localScale = scale;
                }
                else if (delta > 0)
                {
                    _direction = Vector3.right;
                    Vector3 scale = transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * 1f;
                    transform.localScale = scale;
                }
            }

            _direction *= _speed;
            _direction.y = _rigidbody2D.velocity.y;
            _rigidbody2D.velocity = _direction;
            _animator.SetFloat(Speed, Mathf.Abs(_direction.x));
        }
    }
}
