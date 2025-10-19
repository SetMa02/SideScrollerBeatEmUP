using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class EnemyMeleeAI : MonoBehaviour
{
   [SerializeField]private float _damage;

   private void OnTriggerEnter2D(Collider2D other)
   {
      if (other.CompareTag("Player"))
      {
         if (other.TryGetComponent(out PlayerCombat2D player))
         {
            Attack(player);
         }
      }
   }

   private void Attack(PlayerCombat2D player)
   {
      player.GetHit(_damage);
   }
}
