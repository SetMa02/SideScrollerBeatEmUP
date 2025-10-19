// Assets/Scripts/Player/PlayerAnimationBridge.cs
using UnityEngine;

[RequireComponent(typeof(PlayerMotor2D))]
public class PlayerAnimationBridge : MonoBehaviour
{
    public Animator animator;

    [Header("Params")]
    public string pSpeedX = "SpeedX";
    public string pSpeedY = "SpeedY";
    public string pGround = "IsGrounded";
    public string pRun = "IsRunning";
    public string pFall = "IsFalling";

    PlayerMotor2D _motor;

    void Awake() { _motor = GetComponent<PlayerMotor2D>(); }

    void Update()
    {
        if (!animator) return;

        var v = _motor.Velocity;
        animator.SetFloat(pSpeedX, Mathf.Abs(v.x));
        animator.SetFloat(pSpeedY, v.y);
        animator.SetBool(pGround, _motor.IsGrounded);
        animator.SetBool(pRun, _motor.IsRunning);
        animator.SetBool(pFall, v.y < -0.1f);
    }
}
