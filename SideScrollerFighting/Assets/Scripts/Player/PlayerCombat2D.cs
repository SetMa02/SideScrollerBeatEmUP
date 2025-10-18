// Assets/Scripts/Player/PlayerCombat2D.cs
using UnityEngine;

[RequireComponent(typeof(PlayerMotor2D))]
public class PlayerCombat2D : MonoBehaviour
{
    [Header("Attack")]
    public KeyCode attackKey = KeyCode.Mouse0;
    public float attackCooldown = 0.35f;

    [Header("Hitbox")]
    public AttackHitbox hitbox;   // ������ �� ���������-������� (�� �������� �������)
    public float hitboxEnableTime = 0.08f; // ����� ������� ������� (���� ��� ���� �������)
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

        // ���� �� ����������� Animation Events � �������� ��������/��������� �������
        if (hitbox)
        {
            CancelInvoke(nameof(EnableHitbox));
            CancelInvoke(nameof(DisableHitbox));
            Invoke(nameof(EnableHitbox), hitboxEnableTime);
            Invoke(nameof(DisableHitbox), hitboxDisableTime);
            // ��������� ����� ���� �����
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

    // ��� ������ ����� ������� �� Animation Events
    public void AE_AttackStart() => IsAttacking = true;
    public void AE_HitboxOn() => EnableHitbox();
    public void AE_HitboxOff() => DisableHitbox();
    public void AE_AttackEnd() => EndAttack();

}

