using Fusion;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("이동 속도 설정")]
    [SerializeField] private float walkSpeed = 3f;      // 기본 걷기 속도
    [SerializeField] private float runSpeed = 6f;       // 뛰기 속도
    [SerializeField] private float crouchSpeed = 2f;    // 앉기 속도
    [SerializeField] private float jumpHeight = 1.2f;   // 점프 높이
    [SerializeField] private float gravity = -9.81f;    // 중력 값

    [Header("가속 / 감속 설정")]
    [SerializeField] private float acceleration = 10f;  // 가속도
    [SerializeField] private float deceleration = 10f;  // 감속도
    [SerializeField] private float airControl = 5f;     // 공중에서의 제어력

    [Header("앉기 설정")]
    [SerializeField] private float standingHeight = 2f;     // 서 있을 때 높이
    [SerializeField] private float crouchingHeight = 1f;    // 앉았을 때 높이

    private CharacterController controller;
    private Animator animator;
    private CameraController localCam;

    // ⭐ 네트워크 동기화 변수들
    [Networked] public float NetworkedYaw { get; set; }
    [Networked] private Vector3 NetworkedVelocity { get; set; }           // 수직 속도(중력/점프)
    [Networked] private Vector3 NetworkedMoveVelocity { get; set; }       // 이동 벡터
    [Networked] private NetworkBool NetworkedIsGrounded { get; set; }     // 땅에 닿았는지
    [Networked] private NetworkBool NetworkedIsCrouching { get; set; }    // 앉은 상태인지

    // 로컬 변수 (계산용)
    private Vector3 velocity;
    private Vector3 currentMoveVelocity;
    private bool isGrounded;
    private bool isCrouching;

    // 네트워크로 스폰될 때 호출됨
    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();
        // netCC = GetComponent<NetworkCharacterController>();
        animator = GetComponent<Animator>();

        standingHeight = controller.height;

        // 로컬 플레이어일 때만 카메라를 연결
        if (Object.HasInputAuthority)
            StartCoroutine(SetupCamera());
    }

    // 카메라 연결 지연 처리 (Fusion 초기화 완료 기다림)
    private IEnumerator SetupCamera()
    {
        yield return new WaitForSeconds(0.2f);

        // 로컬 플레이어만 카메라 연결
        if (!Object.HasInputAuthority)
            yield break;
        
        Transform pivot = transform.Find("CameraPivot");

        // CameraController가 생성/활성화될 때까지 계속 대기
        while (localCam == null)
        {
            var camCtrl = GetComponentInChildren<CameraController>(true);
            if (camCtrl != null)
            {
                camCtrl.cameraPivot = pivot;
                localCam = camCtrl;
                Debug.Log("[Fusion] FPS 카메라 Pivot 연결 완료");
                yield break;
            }

        yield return null; // 다음 프레임에서 다시 탐색
    }

        // // Camera.main 제거 !!
        // var camCtrl = GetComponentInChildren<CameraController>();
        // if (camCtrl != null)
        // {
        //     camCtrl.cameraPivot = transform.Find("CameraPivot");
        //     localCam = camCtrl;   // ⭐ 로컬 카메라 저장
        //     Debug.Log("[Fusion] FPS 카메라 Pivot 연결 완료");
        // }
        // else
        // {
        //     Debug.LogError("[Fusion] CameraController를 찾지 못했습니다!");
        // }
    }

    // Fusion의 FixedUpdateNetwork() — 네트워크 프레임마다 실행됨
    public override void FixedUpdateNetwork()
    {
        // ⭐ StateAuthority(서버/호스트)만 물리 시뮬레이션 실행
        // CharacterController는 Transform을 직접 수정하므로 StateAuthority에서만 실행해야 함
        if (Object.HasStateAuthority)
        {
            // InputAuthority가 있는 플레이어의 입력만 가져옴
            if (GetInput(out NetworkInputData data))
            {
                HandleMovement(data);
            }
        }

        // 모든 클라이언트에서 애니메이션 실행 (NetworkTransform이 위치 동기화)
        HandleAnimation();
    }

    //   이동 처리
    private void HandleMovement(NetworkInputData data)
    {
        // ⭐ Input Authority를 가진 로컬 플레이어만 카메라가 필요함
        if (Object.HasInputAuthority && localCam == null)
            return; // 카메라 초기화 전에는 이동 처리 안 함

        // --- 땅 체크 ---
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;  // 땅을 붙잡는 용도

        // --- 앉기 처리 ---
        if (data.crouchHeld && !isCrouching)
        {
            isCrouching = true;
            controller.height = crouchingHeight;
            controller.center = new Vector3(0, crouchingHeight / 2, 0);
        }
        else if (!data.crouchHeld && isCrouching)
        {
            isCrouching = false;
            controller.height = standingHeight;
            controller.center = new Vector3(0, standingHeight / 2, 0);
        }

        // --- 캐릭터 회전 및 이동 방향 계산 ---
        // ⭐ 입력 권한이 있는 플레이어만 Yaw 업데이트
        Debug.Log($"[PlayerMovement] HandleMovement 시작 | HasInputAuthority: {Object.HasInputAuthority} | data.cameraYaw: {data.cameraYaw:F1}");

        if (Object.HasInputAuthority)
        {
            Debug.Log($"[PlayerMovement] ⭐ NetworkedYaw 업데이트 전: {NetworkedYaw:F1} → 후: {data.cameraYaw:F1}");
            NetworkedYaw = data.cameraYaw;
        }

        // 디버그 로그
        if (Object.HasInputAuthority)
        {
            Debug.Log($"[Local] Player {Object.InputAuthority} | Yaw: {NetworkedYaw:F1} | Input: {data.moveInput} | Pos: {transform.position}");
        }
        else
        {
            Debug.Log($"[Remote] Player {Object.InputAuthority} | Yaw: {NetworkedYaw:F1} | Input: {data.moveInput} | Pos: {transform.position}");
        }

        // 모든 플레이어가 네트워크 동기화된 Yaw 기준으로 회전
        transform.rotation = Quaternion.Euler(0, NetworkedYaw, 0);

        // 이동 방향 계산 (NetworkedYaw 기준)
        Quaternion yawRotation = Quaternion.Euler(0, NetworkedYaw, 0);
        Vector3 forward = yawRotation * Vector3.forward;
        Vector3 right = yawRotation * Vector3.right;
        Vector3 inputDirection = right * data.moveInput.x + forward * data.moveInput.y;

        // --- 이동 속도 결정 (걷기 / 뛰기 / 앉기) ---
        float targetSpeed =
            isCrouching ? crouchSpeed :
            data.runHeld ? runSpeed :
            walkSpeed;

        Vector3 targetVelocity = inputDirection * targetSpeed;

        // --- 가속/감속 적용 ---
        if (isGrounded)
        {
            float factor = inputDirection.magnitude > 0 ? acceleration : deceleration;
            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, factor * Runner.DeltaTime);
        }
        else
        {
            // 공중에서 이동 입력이 있을 때만 제어
            if (inputDirection.magnitude > 0)
            {
                currentMoveVelocity =
                    Vector3.Lerp(currentMoveVelocity, targetVelocity, airControl * Runner.DeltaTime);
            }
        }

        // 실제 이동 적용 (CharacterController가 Transform 직접 변경)
        controller.Move(currentMoveVelocity * Runner.DeltaTime);

        // --- 점프 처리 ---
        if (data.jumpPressed && isGrounded && !isCrouching)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // --- 중력 적용 ---
        velocity.y += gravity * Runner.DeltaTime;
        controller.Move(velocity * Runner.DeltaTime);

        // ⭐ 네트워크 변수 업데이트 (StateAuthority에서만 실행됨)
        NetworkedVelocity = velocity;
        NetworkedMoveVelocity = currentMoveVelocity;
        NetworkedIsGrounded = isGrounded;
        NetworkedIsCrouching = isCrouching;

        // ⭐ CharacterController가 Transform을 직접 움직이고
        // NetworkTransform이 자동으로 위치/회전을 모든 클라이언트에 동기화
        // Networked 변수들도 자동 동기화됨
    }

    //   애니메이션 처리
    private void HandleAnimation()
    {
        if (animator == null) return;

        // ⭐ StateAuthority에서 업데이트한 네트워크 변수를 사용
        // 모든 클라이언트가 동기화된 값으로 애니메이션 실행
        Vector3 moveVel = Object.HasStateAuthority ? currentMoveVelocity : NetworkedMoveVelocity;
        bool grounded = Object.HasStateAuthority ? isGrounded : NetworkedIsGrounded;
        bool crouching = Object.HasStateAuthority ? isCrouching : NetworkedIsCrouching;

        // 현재 이동 벡터를 로컬 공간 기준으로 변환
        Vector3 localVel = transform.InverseTransformDirection(moveVel);

        animator.SetFloat("Horizontal", localVel.x);
        animator.SetFloat("Vertical", localVel.z);

        animator.SetBool("IsGrounded", grounded);
        animator.SetBool("IsCrouching", crouching);
        animator.SetBool("IsJumping", !grounded);

        // Speed(걷기/뛰기 전환값) — 로컬 플레이어에게만 적용
        float speedParam = Object.HasInputAuthority && Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;
        animator.SetFloat("Speed", speedParam);
    }
}
