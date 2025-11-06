using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("기본 설정")]
    public Transform playerTransform;
    public Vector3 offset = new Vector3(0f, 3f, -6f);
    public float rotationSpeed = 450f;


    [Header("조준 모드")]
    public bool isAiming = false;
    public Vector3 aimOffset = new Vector3(0f, 2f, -3f);
    public float aimTransitionSpeed = 5f;
    public float aimSensitivity = 0.5f;

    [Header("사망 시점")]
    public bool isDeathView = false;
    private Quaternion deathViewRotation;

    private float mouseX, mouseY;
    private Camera cam;

    void Start()
    {
        if (playerTransform == null)
            playerTransform = GameObject.Find("Player").transform;

        cam = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseRotation();
        HandleAimInput();
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        if (isDeathView)
        {
            // 사망 시 상공 시점 고정
            transform.position = Vector3.Lerp(transform.position, playerTransform.position + new Vector3(0, 15f, -10f), Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, deathViewRotation, Time.deltaTime);
            return;
        }

        // 카메라 위치 계산 (조준 여부에 따라 오프셋 변경)
        Vector3 desiredOffset = isAiming ? aimOffset : offset;
        Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0);
        Vector3 targetPosition = playerTransform.position + rotation * desiredOffset;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * aimTransitionSpeed);
        // transform.LookAt(playerTransform);
        Quaternion lookRotation = Quaternion.LookRotation(playerTransform.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

    }

    private void HandleMouseRotation()
    {
        if (isDeathView) return;

        // 마우스 감도
        float sens = isAiming ? aimSensitivity : rotationSpeed;

        mouseX += Input.GetAxis("Mouse X") * sens * 0.002f;
        mouseY -= Input.GetAxis("Mouse Y") * sens * 0.002f;
        mouseY = Mathf.Clamp(mouseY, -35f, 60f);
    }

    private void HandleAimInput()
    {
        isAiming = Input.GetMouseButton(1);

        // 조준 시 FOV 조정 (줌인)
        float targetFov = isAiming ? 40f : 60f;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * 5f);
    }

    // --------- 이벤트 핸들러 ---------
    public void OnPlayerDeath()
    {
        isDeathView = true;
        deathViewRotation = Quaternion.Euler(45f, 0f, 0f);
    }

    public void OnPlayerRespawn()
    {
        isDeathView = false;
    }
}
