using UnityEngine;

/// <summary>
/// Child hitbox component that forwards damage to the owning PlayerState with a multiplier.
/// Attach separate instances (e.g., head/body) to trigger colliders under the Player root.
/// </summary>
[DisallowMultipleComponent]
public class PlayerHitbox : MonoBehaviour
{
    [Tooltip("Additional damage multiplier applied when this hitbox is struck. Head=2, Body=1, etc.")]
    [SerializeField] private float damageMultiplier = 1f;

    private PlayerState playerState;

    private void Awake()
    {
        playerState = GetComponentInParent<PlayerState>();
    }

    /// <summary>
    /// Apply damage to the owning player using the configured multiplier.
    /// </summary>
    public void ApplyDamage(float baseDamage, int attackerId)
    {
        if (playerState == null || playerState.IsDead)
            return;

        float finalDamage = Mathf.Max(0f, baseDamage * damageMultiplier);
        playerState.RPC_TakeDamage(finalDamage, attackerId);
    }

    /// <summary>
    /// Utility accessor for external systems (e.g., UI/debugging).
    /// </summary>
    public float GetMultiplier() => damageMultiplier;
}