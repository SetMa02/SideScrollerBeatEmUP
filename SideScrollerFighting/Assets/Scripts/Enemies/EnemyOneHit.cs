// Assets/Scripts/Enemies/EnemyOneHit.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyOneHit : MonoBehaviour, IDamageable
{
    public bool destroyOnDeath = true;
    public GameObject deathFx; // �����������

    bool _alive = true;
    public bool IsAlive => _alive;

    public void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        if (!_alive) return;
        _alive = false;

        if (deathFx)
            Instantiate(deathFx, hitPoint, Quaternion.identity);

        // ����� ����� ��������� AI/������/������-�������
        if (destroyOnDeath)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
