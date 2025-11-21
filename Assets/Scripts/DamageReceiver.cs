using UnityEngine;

/// <summary>
/// Centralized damage handler for a player. Attach this to the player root
/// and connect child <see cref="HitBox"/> components to apply per-part multipliers
/// without changing the main CharacterController collider.
/// </summary>
[DisallowMultipleComponent]
public class DamageReceiver : MonoBehaviour
{
    [SerializeField, Tooltip("Optional PlayerState to forward damage to. If null, the component tries to find one in parents.")]
    private PlayerState playerState;

    private void Awake()
    {
        if (playerState == null)
            playerState = GetComponent<PlayerState>() ?? GetComponentInParent<PlayerState>();
    }

    /// <summary>
    /// Applies damage to the owning player (if available).
    /// </summary>
    public void ApplyDamage(float amount, int attackerId)
    {
        if (playerState != null && !playerState.IsDead)
        {
            playerState.RPC_TakeDamage(amount, attackerId);
        }
    }
}