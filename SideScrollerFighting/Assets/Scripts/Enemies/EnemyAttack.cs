using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class EnemyAttack : MonoBehaviour
{
    public UnityEvent<float> Attack;
    
    private BoxCollider2D _attackCollider;
    private float _damage;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Attack.Invoke(_damage);
            Debug.Log("Attack Player");
        }
       
    }
}
