using UnityEngine;

[RequireComponent(typeof(PlayerMotor2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerSlide : MonoBehaviour
{
    [Header("Input & Basic")]
    public KeyCode slideKey = KeyCode.LeftControl;
    public float minRunSpeed = 2.5f;
    public float slideSpeed = 9f;
    public float slideDuration = 0.32f;
    public float slideCooldown = 0.5f;

    [Header("Collider Resize (optional)")]
    public bool resizeCollider = true;
    public Vector2 slideColliderSize = new(0.9f, 1.0f);
    public Vector2 slideColliderOffset = new(0f, -0.35f);

    [Header("Invulnerability (i-frames)")]
    public bool invulnerable = true;

    [Tooltip("Безопасный режим: игнорируем коллизии Player↔вражеские слои без смены слоя игрока")]
    public bool useLayerIgnore = true;

    [Tooltip("Имя слоя игрока (для IgnoreLayerCollision). Если пусто, возьмём фактический слой объекта.")]
    public string playerLayerName = "Player";

    [Tooltip("С какими слоями временно игнорировать столкновения во время подката")]
    public string[] ignoreWithLayers = { "Enemy", "EnemyAttack", "Hazard", "Projectile" };

    [Tooltip("АЛЬТЕРНАТИВА (необязательно): смена слоя на время подката")]
    public string invulnLayerName = "Invulnerable";
    [Range(-1, 31)] public int invulnLayerIndex = -1;

    [Header("Animator (optional)")]
    public Animator animator;
    public string trigSlide = "DoRoll";

    PlayerMotor2D _motor;
    Collider2D _col;
    Vector2 _origSize, _origOffset;
    int _origLayer;
    bool _sliding;
    float _cdTimer;

    int _playerLayer;       // фактический индекс слоя игрока
    int[] _ignoredLayers;   // валидные индексы слоёв для игнора

    void Awake()
    {
        _motor = GetComponent<PlayerMotor2D>();
        _col = GetComponent<Collider2D>();
        _origLayer = gameObject.layer;

        if (_col is CapsuleCollider2D cap) { _origSize = cap.size; _origOffset = cap.offset; }
        else if (_col is BoxCollider2D box) { _origSize = box.size; _origOffset = box.offset; }

        // 1) Слой игрока: если имя не найдено — используем фактический слой объекта
        _playerLayer = gameObject.layer;
        if (!string.IsNullOrEmpty(playerLayerName))
        {
            int byName = LayerMask.NameToLayer(playerLayerName);
            if (byName >= 0 && byName <= 31) _playerLayer = byName;
        }

        // 2) Преобразуем ignoreWithLayers в индексы и отфильтровываем несуществующие
        _ignoredLayers = new int[ignoreWithLayers.Length];
        for (int i = 0; i < ignoreWithLayers.Length; i++)
        {
            int idx = LayerMask.NameToLayer(ignoreWithLayers[i]);
            _ignoredLayers[i] = (idx >= 0 && idx <= 31) ? idx : -1;
        }
    }

    void Update()
    {
        _cdTimer -= Time.deltaTime;
        if (_sliding) return;

        if (Input.GetKeyDown(slideKey)
            && _cdTimer <= 0f
            && _motor.IsGrounded
            && Mathf.Abs(_motor.Velocity.x) >= minRunSpeed)
        {
            StartSlide();
        }
    }

    public void AE_SlideStart() => StartSlide();
    public void AE_SlideEnd() => EndSlide();

    void StartSlide()
    {
        if (_sliding) return;
        _sliding = true;
        _cdTimer = slideCooldown;

        if (animator) { animator.ResetTrigger(trigSlide); animator.SetTrigger(trigSlide); }

        float dir = _motor.FacingRight ? 1f : -1f;
        _motor.SetMovementLock(true);
        _motor.ApplyExternalVelocityX(dir * slideSpeed, slideDuration);

        // i-frames
        if (invulnerable)
        {
            if (useLayerIgnore)
            {
                // Игнорируем только валидные пары слоёв
                if (_playerLayer < 0 || _playerLayer > 31)
                {
                    Debug.LogWarning("[PlayerSlide] Некорректный слой игрока. Проверь playerLayerName и слой объекта Player.");
                }
                else
                {
                    for (int i = 0; i < _ignoredLayers.Length; i++)
                    {
                        int enemyLay = _ignoredLayers[i];
                        if (enemyLay < 0 || enemyLay > 31) continue; // пропускаем несуществующий
                        Physics2D.IgnoreLayerCollision(_playerLayer, enemyLay, true);
                    }
                }
            }
            else
            {
                int targetLayer = (invulnLayerIndex >= 0) ? invulnLayerIndex : LayerMask.NameToLayer(invulnLayerName);
                if (targetLayer >= 0 && targetLayer <= 31)
                {
                    gameObject.layer = targetLayer;
                }
                else
                {
                    Debug.LogWarning($"[PlayerSlide] Слой '{invulnLayerName}' не найден. Создайте его или включите useLayerIgnore.");
                }
            }
        }

        if (resizeCollider)
        {
            if (_col is CapsuleCollider2D cap) { cap.size = slideColliderSize; cap.offset = slideColliderOffset; }
            else if (_col is BoxCollider2D box) { box.size = slideColliderSize; box.offset = slideColliderOffset; }
        }

        Invoke(nameof(EndSlide), slideDuration * 0.98f);
    }

    void EndSlide()
    {
        if (!_sliding) return;
        _sliding = false;

        _motor.SetMovementLock(false);

        // ЖЁСТКО останавливаем горизонтально
        var rb = GetComponent<Rigidbody2D>();
        rb.velocity = new Vector2(0f, rb.velocity.y);
        if (!_sliding) return;
        _sliding = false;

        _motor.SetMovementLock(false);

        if (invulnerable)
        {
            if (useLayerIgnore)
            {
                if (_playerLayer >= 0 && _playerLayer <= 31)
                {
                    for (int i = 0; i < _ignoredLayers.Length; i++)
                    {
                        int enemyLay = _ignoredLayers[i];
                        if (enemyLay < 0 || enemyLay > 31) continue;
                        Physics2D.IgnoreLayerCollision(_playerLayer, enemyLay, false);
                    }
                }
            }
            else
            {
                gameObject.layer = _origLayer;
            }
        }

        if (resizeCollider)
        {
            if (_col is CapsuleCollider2D cap) { cap.size = _origSize; cap.offset = _origOffset; }
            else if (_col is BoxCollider2D box) { box.size = _origSize; box.offset = _origOffset; }
        }
    }
}
