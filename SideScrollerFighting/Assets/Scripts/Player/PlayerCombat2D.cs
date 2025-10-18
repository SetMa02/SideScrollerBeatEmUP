// Assets/Scripts/Player/PlayerCombat2D.cs
using UnityEngine;

[RequireComponent(typeof(PlayerMotor2D))]
public class PlayerCombat2D : MonoBehaviour
{
    [Header("Attack")]
    public KeyCode attackKey = KeyCode.Mouse0;
    public float attackCooldown = 0.35f;

    [Header("Hitbox")]
    public AttackHitbox hitbox;   // ссылка на компонент-хитбокс (на дочернем объекте)
    public float hitboxEnableTime = 0.08f; // когда открыть хитбокс (если нет аним эвентов)
    public float hitboxDisableTime = 0.18f;

    [Header("Animator")]
    public Animator animator;
    public string trigAttack = "Attack";

    float _cooldownTimer;

    public bool IsAttacking { get; private set; }

    void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(attackKey) && _cooldownTimer <= 0f)
        {
            _cooldownTimer = attackCooldown;
            StartAttack();
        }
    }

    void StartAttack()
    {
        IsAttacking = true;
        if (animator) animator.SetTrigger(trigAttack);

        // Если не используешь Animation Events — таймером включаем/выключаем хитбокс
        if (hitbox)
        {
            CancelInvoke(nameof(EnableHitbox));
            CancelInvoke(nameof(DisableHitbox));
            Invoke(nameof(EnableHitbox), hitboxEnableTime);
            Invoke(nameof(DisableHitbox), hitboxDisableTime);
            // окончание атаки чуть позже
            Invoke(nameof(EndAttack), attackCooldown * 0.9f);
        }
        else
        {
            Invoke(nameof(EndAttack), attackCooldown);
        }
    }
    


    void EnableHitbox() { if (hitbox) hitbox.SetActive(true); }
    void DisableHitbox() { if (hitbox) hitbox.SetActive(false); }

    void EndAttack() { IsAttacking = false; }

    // Эти методы можно дергать из Animation Events
    public void AE_AttackStart() => IsAttacking = true;
    public void AE_HitboxOn() => EnableHitbox();
    public void AE_HitboxOff() => DisableHitbox();
    public void AE_AttackEnd() => EndAttack();

}

