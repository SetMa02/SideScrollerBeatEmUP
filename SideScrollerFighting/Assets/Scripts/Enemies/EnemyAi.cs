using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStatus))]
public class EnemyAi : MonoBehaviour
{
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float attackRange = 1.2f;
    public float visionRange = 6f;
    public Transform[] patrolPoints;
    private int _currentPatrolIndex = 0;

    private Transform _player;
    private Animator _animator;
    private Rigidbody2D _rb;
    private bool _chasing = false;
    private bool _attacking = false;
    private Vector2 _direction;
    private EnemyAttack _enemyAttack;
    private EnemyStatus  _status;

    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _direction = new Vector2();
        _enemyAttack = GetComponentInChildren<EnemyAttack>();
        _enemyAttack.Attack.AddListener(Attack);
    }

    private void ChasePlayer()
    {
        
    }

    private void Attack(float damege)
    {
        Debug.Log("Is attacking");
        _animator.SetTrigger("Attack");
    }
}
