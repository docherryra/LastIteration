using UnityEngine;

/// <summary>
/// Detects incoming damage on a specific collider (e.g., head/body) and forwards
/// it to the parent <see cref="DamageReceiver"/> with a configurable multiplier.
/// Attach this to trigger/collision hitbox children so the main CharacterController
/// collider can remain unchanged.
/// </summary>
[DisallowMultipleComponent]
public class HitBox : MonoBehaviour
{
    [SerializeField, Tooltip("Damage multiplier for this hitbox (e.g., Head=2, Body=1).")]
    private float damageMultiplier = 1f;

    private DamageReceiver receiver;

    private void Awake()
    {
        receiver = GetComponentInParent<DamageReceiver>();
        if (receiver == null)
        {
            Debug.LogWarning($"[HitBox] No DamageReceiver found in parents for {name}. Damage will be ignored.");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryApplyDamage(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyDamage(other);
    }

    private void TryApplyDamage(Collider other)
    {
        if (receiver == null)
            return;

        var bullet = other.GetComponent<Bullet>() ?? other.GetComponentInParent<Bullet>();
        if (bullet != null)
        {
            receiver.ApplyDamage(bullet.damage * damageMultiplier, bullet.shooterId);
            Destroy(bullet.gameObject);
        }
    }
}