// PlayerCombat2D.cs — расширенная версия: лочим движение, спец-атака, два хитбокса
using UnityEngine;

[RequireComponent(typeof(PlayerMotor2D))]
public class PlayerCombat2D : MonoBehaviour
{
    [Header("Common")]
    public Animator animator;
    PlayerMotor2D _motor;

    [Header("Normal Attack")]
    public KeyCode attackKey = KeyCode.Mouse0;
    public float attackCooldown = 0.35f;
    public AttackHitbox hitbox;                 // обычный хитбокс
    public float hitboxEnableTime = 0.08f;
    public float hitboxDisableTime = 0.18f;

    [Header("Special Attack")]
    public KeyCode specialKey = KeyCode.Mouse1;
    public float specialCooldown = 0.8f;
    public AttackHitbox specialHitbox;          // отдельный хитбокс (больше урон/радиус)
    public float spHitOnTime = 0.14f;
    public float spHitOffTime = 0.28f;

    [Header("Animator Triggers")]
    public string trigAttack = "Attack";
    public string trigSpecial = "Special";

    float _atkCd;
    float _spCd;
    bool _isAttacking;

    void Awake()
    {
        _motor = GetComponent<PlayerMotor2D>();
    }

    void Update()
    {
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
    }

    void StartAttack()
    {
        if (animator) { animator.ResetTrigger(trigAttack); animator.SetTrigger(trigAttack); }
        _isAttacking = true;

        // Лочим движение на всю атаку
        _motor.SetMovementLock(true);

        if (hitbox)
        {
            CancelInvoke(nameof(HB_On));
            CancelInvoke(nameof(HB_Off));
            Invoke(nameof(HB_On), hitboxEnableTime);
            Invoke(nameof(HB_Off), hitboxDisableTime);
        }
        // Фэйлсейф: авторазлочка в конце КД (если забыли эвенты)
        Invoke(nameof(EndAttack), attackCooldown * 0.95f);
    }

    void StartSpecial()
    {
        if (animator) { animator.ResetTrigger(trigSpecial); animator.SetTrigger(trigSpecial); }
        _isAttacking = true;
        _motor.SetMovementLock(true);

        if (specialHitbox)
        {
            CancelInvoke(nameof(SP_On));
            CancelInvoke(nameof(SP_Off));
            Invoke(nameof(SP_On), spHitOnTime);
            Invoke(nameof(SP_Off), spHitOffTime);
        }
        Invoke(nameof(EndAttack), specialCooldown * 0.95f);
    }

    void HB_On() { if (hitbox) hitbox.SetActive(true); }
    void HB_Off() { if (hitbox) hitbox.SetActive(false); }

    void SP_On() { if (specialHitbox) specialHitbox.SetActive(true); }
    void SP_Off() { if (specialHitbox) specialHitbox.SetActive(false); }

    void EndAttack()
    {
        _isAttacking = false;
        _motor.SetMovementLock(false);
        if (hitbox) hitbox.SetActive(false);
        if (specialHitbox) specialHitbox.SetActive(false);
    }

    // Animation Events (если используешь эвенты в клипах)
    public void AE_AttackStart() { _isAttacking = true; _motor.SetMovementLock(true); }
    public void AE_HitboxOn() { HB_On(); }
    public void AE_HitboxOff() { HB_Off(); }
    public void AE_AttackEnd() { EndAttack(); }

    public void AE_SpecialStart() { _isAttacking = true; _motor.SetMovementLock(true); }
    public void AE_SpecialOn() { SP_On(); }
    public void AE_SpecialOff() { SP_Off(); }
    public void AE_SpecialEnd() { EndAttack(); }
}
