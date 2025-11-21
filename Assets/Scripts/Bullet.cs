using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifetime = 3f;

    [Header("Combat")]
    public float damage = 34f;
    public int shooterId = -1;  // Gun에서 설정

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        Destroy(gameObject, lifetime);
        if (rb != null) rb.velocity = transform.forward * speed;
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider target)
    {
        // 우선 히트박스가 있다면 멀티플라이어 적용
        var hitbox = target.GetComponent<PlayerHitbox>() ?? target.GetComponentInParent<PlayerHitbox>();
        if (hitbox != null)
        {
            hitbox.ApplyDamage(damage, shooterId);
            Destroy(gameObject);
            return;
        }

        // 히트박스가 없으면 기존 플레이어 상태를 직접 찾음
        var state = target.GetComponentInParent<PlayerState>();
        if (state != null && !state.IsDead)
        {
            state.RPC_TakeDamage(damage, shooterId);
        }

        Destroy(gameObject);
    }
}
