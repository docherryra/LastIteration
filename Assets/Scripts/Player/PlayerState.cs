using Fusion;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [Networked] public float Hp { get; set; } = 100f;
    [Networked] public float MaxHp { get; set; } = 100f;
    [Networked] public float Kill { get; set; } = 0f;
    [Networked] public float Death { get; set; } = 0f;
    [Networked] public NetworkBool IsDead { get; set; } = false;
    [Networked] private TickTimer RespawnTimer { get; set; }

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 3f;

    // ★ 리스폰 가장자리 길이 (x, z 절댓값 14)
    [Header("Respawn Area")]
    [SerializeField] private float respawnEdge = 14f;

    private Collider[] colliders;
    private Renderer[] renderers;
    private Rigidbody rb;
    private Animator animator;
    private CharacterController characterController;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Hp = MaxHp;
            IsDead = false;
        }

        colliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }

    // ★ 테스트용: L 키 눌러서 강제로 죽어보기
    private void Update()
    {
        // 입력 권한 있는 로컬 클라이언트만
        if (!Object.HasInputAuthority) return;
        if (IsDead) return;

        if (Input.GetKeyDown(KeyCode.L))
        {
            // 공격자 ID는 테스트라 -1 넣어둠
            RPC_TakeDamage(9999f, -1);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority && IsDead && RespawnTimer.ExpiredOrNotRunning(Runner))
        {
            Respawn();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage, int attackerId)
    {
        if (IsDead) return;

        Hp -= Mathf.Max(0f, damage);

        if (Hp <= 0f)
        {
            Die(attackerId);
        }
    }

    private void Die(int attackerId)
    {
        if (!Object.HasStateAuthority || IsDead) return;

        IsDead = true;
        Death += 1f;

        RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnDelay);

        RPC_PlayDeathAnimation();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsDead", true);
            animator.SetTrigger("Die");
        }

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        if (colliders != null)
        {
            foreach (var c in colliders) c.enabled = false;
        }
    }

    private void Respawn()
    {
        if (!Object.HasStateAuthority) return;

        // ★ 가장자리 사각형 위의 랜덤 위치
        Vector3 respawnPos = GetRandomEdgePosition();

        // ★ (0,0,0)을 바라보는 회전값
        Quaternion respawnRot = GetLookAtCenterRotation(respawnPos);

        Hp = MaxHp;
        IsDead = false;

        RPC_PlayRespawnAnimation(respawnPos, respawnRot);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayRespawnAnimation(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

        if (animator != null)
        {
            animator.SetBool("IsDead", false);
            animator.Rebind();
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        SetAliveVisual(true);
    }

    private void SetAliveVisual(bool alive)
    {
        if (colliders != null)
            foreach (var c in colliders) c.enabled = alive;

        if (renderers != null)
            foreach (var r in renderers) r.enabled = alive;
    }

    public float GetRespawnRemaining()
    {
        if (!IsDead || RespawnTimer.ExpiredOrNotRunning(Runner))
            return 0f;

        return RespawnTimer.RemainingTime(Runner) ?? 0f;
    }

    public float GetKill() => Kill;
    public float GetDeath() => Death;
    public float GetHp() => Hp;
    public float GetMaxHp() => MaxHp;

    // ★ 가장자리 사각형(네 변) 위의 랜덤 위치 계산
    //   x, z 절댓값이 최대 14가 되도록 설정
    private Vector3 GetRandomEdgePosition()
    {
        float edge = respawnEdge;
        float y = transform.position.y; // y는 현재 높이 유지 (원하면 따로 SerializeField로 빼도 됨)

        // -edge ~ edge 구간 중 하나
        float t = Random.Range(-edge, edge);

        // 0: +x변, 1: -x변, 2: +z변, 3: -z변
        int side = Random.Range(0, 4);

        float x = 0f;
        float z = 0f;

        switch (side)
        {
            case 0: // x = +edge, z는 -edge~edge
                x = edge;
                z = t;
                break;
            case 1: // x = -edge
                x = -edge;
                z = t;
                break;
            case 2: // z = +edge
                z = edge;
                x = t;
                break;
            case 3: // z = -edge
                z = -edge;
                x = t;
                break;
        }

        return new Vector3(x, y, z);
    }

    // ★ 주어진 위치에서 (0,0,0)을 바라보는 회전값
    private Quaternion GetLookAtCenterRotation(Vector3 position)
    {
        Vector3 dir = Vector3.zero - position; // 중심 - 내 위치
        dir.y = 0f;                            // 위/아래는 무시하고 수평만 보게

        if (dir.sqrMagnitude < 0.0001f)
        {
            // 혹시 (0,0,0)에 너무 가까우면 기본 방향
            dir = Vector3.forward;
        }

        return Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}
