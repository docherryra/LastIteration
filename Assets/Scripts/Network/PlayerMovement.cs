using System.Collections;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

// 자신의 플레이어가 자기 클라이언트에서만 움직이도록 
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpForce = 5f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();

        // 내 플레이어일 때만 카메라 연결
        if (Object.HasInputAuthority)
            StartCoroutine(SetupCamera());
        // Camera.main.GetComponent<CameraFollow>().SetTarget(transform);

    }
    
    private IEnumerator SetupCamera()
    {
        yield return new WaitForSeconds(0.2f); // 네트워크 초기화 대기
        var cam = Camera.main?.GetComponent<CameraController>();
        
        if (cam != null) {
            cam.playerTransform = transform;
            Debug.Log("[Fusion] CameraController 연결 완료");

        } else {
            Debug.LogWarning("[Fusion] CameraController를 찾을 수 없습니다.");
        }
    }

    public override void FixedUpdateNetwork()
{
    if (GetInput(out NetworkInputData data))
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        Vector3 move = transform.right * data.moveInput.x + transform.forward * data.moveInput.y;
        controller.Move(move * moveSpeed * Runner.DeltaTime);

        if (data.jumpPressed && isGrounded)
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

        velocity.y += gravity * Runner.DeltaTime;
        controller.Move(velocity * Runner.DeltaTime);
    }
}
}
