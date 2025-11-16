using UnityEngine;
using Fusion;

public class CameraController : NetworkBehaviour
{
    [Header("FPS 카메라 설정")]
    public Transform cameraPivot;      // 플레이어 머리 위치
    public float mouseSensitivity = 2f;
    public bool isAiming = false;
    public float minPitch = -70f;
    public float maxPitch = 80f;

    private float yaw;
    private float pitch;
    private Camera cam;
    private PlayerMovement playerMovement;

    public override void Spawned()
    {
        // 로컬 플레이어만 카메라 활성화
        if (!Object.HasInputAuthority)
        {
            gameObject.SetActive(false);
            enabled = false;
        }

        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    void Start()
    {
        cam = GetComponent<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraPivot == null)
            Debug.LogError("[CameraController] cameraPivot이 연결되지 않았습니다!");
    }

    void Update()
    {
        if (!Object.HasInputAuthority) return;

        HandleMouseLook();
        HandleCameraPosition();
    }

    private void HandleMouseLook()
    {
        // 마우스 입력
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;

        isAiming = Input.GetMouseButton(1);

        // 상하 회전 제한
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 카메라 회전 적용 (Pitch만)
        transform.localRotation = Quaternion.Euler(pitch, 0, 0);

        // 플레이어 Yaw 회전
        if (cameraPivot != null && cameraPivot.parent != null)
        {
            // cameraPivot.parent.rotation = Quaternion.Euler(0, yaw, 0);
            if (playerMovement != null) {
                // playerMovement.transform.rotation = Quaternion.Euler(0, yaw, 0);
                cameraPivot.localRotation = Quaternion.Euler(0, yaw, 0);
            }
        }
    }

    private void HandleCameraPosition()
    {
        if (cameraPivot == null) return;

        // FPS 카메라는 항상 머리 위치에서 갱신됨
        transform.position = cameraPivot.position;
    }
}
