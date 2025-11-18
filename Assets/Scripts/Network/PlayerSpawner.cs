using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private GameObject playerPrefab;

    // 플레이어가 룸에 입장했을 때 호출됨
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // ⭐ 서버(Host)만 플레이어 스폰 처리
        if (runner.IsServer)
        {
            Debug.Log($"[Fusion] 서버가 플레이어 {player} 스폰 중...");
            Vector3 spawnPos = new Vector3(
                UnityEngine.Random.Range(-5f, 5f),
                1f,
                UnityEngine.Random.Range(-5f, 5f)
            );

            // NetworkObject 스폰 및 InputAuthority 명시적 설정
            NetworkObject playerObject = runner.Spawn(
                playerPrefab,
                spawnPos,
                Quaternion.identity,
                player  // ⭐ 이 매개변수가 InputAuthority를 자동으로 설정함
            );

            if (playerObject != null)
            {
                Debug.Log($"[Fusion] ✅ Player {player} 스폰 완료 | InputAuthority: {playerObject.InputAuthority} | Pos: {spawnPos}");
            }
            else
            {
                Debug.LogError($"[Fusion] ❌ Player {player} 스폰 실패!");
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Fusion] Player left: {player} 나감");
    }

    // 클라이언트의 입력을 Fusion 네트워크로 전달
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        Debug.Log("[PlayerSpawner] OnInput() 호출됨");

        NetworkInputData data = new NetworkInputData();

        //  이동 입력 (WASD)
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        data.moveInput = new Vector2(x, y);

        // 디버그: 입력 확인
        if (x != 0 || y != 0)
        {
            Debug.Log($"[PlayerSpawner] OnInput - Horizontal: {x}, Vertical: {y}");
        }

        data.jumpPressed = Input.GetKeyDown(KeyCode.Space); //  점프 입력
        data.runHeld = Input.GetKey(KeyCode.LeftShift); //  달리기 입력
        data.crouchHeld = Input.GetKey(KeyCode.LeftControl); //  앉기 입력

        // ⭐ 카메라 회전 정보 전달 (로컬 플레이어만)
        // Camera.main은 로컬 플레이어의 카메라만 활성화되어 있음
        data.cameraYaw = 0f; // 기본값

        var cam = Camera.main;
        Debug.Log($"[PlayerSpawner] Camera.main 찾기: {(cam != null ? "성공" : "실패")}");

        if (cam != null)
        {
            // ⭐ Camera와 CameraController가 같은 GameObject에 있음
            var camController = cam.GetComponent<CameraController>();
            Debug.Log($"[PlayerSpawner] Camera.main.GetComponent<CameraController>(): {(camController != null ? "발견" : "없음")}");

            if (camController != null)
            {
                data.cameraYaw = camController.GetYaw();
                Debug.Log($"[PlayerSpawner] ✅ Camera.main에서 Yaw 가져옴: {data.cameraYaw:F1}");
            }
            else
            {
                // Camera가 있지만 CameraController가 없는 경우
                // (다른 카메라일 수 있음 - Scene 카메라 등)
                // 플레이어의 카메라를 직접 찾아서 시도
                Debug.Log("[PlayerSpawner] Camera.main에 CameraController 없음 → FindObjectsOfType로 검색");
                var allCamControllers = FindObjectsOfType<CameraController>();
                Debug.Log($"[PlayerSpawner] 전체 CameraController 개수: {allCamControllers.Length}");

                foreach (var controller in allCamControllers)
                {
                    // InputAuthority를 가진 플레이어의 카메라만 사용
                    var playerMovement = controller.GetComponentInParent<PlayerMovement>();
                    if (playerMovement != null && playerMovement.Object.HasInputAuthority)
                    {
                        data.cameraYaw = controller.GetYaw();
                        Debug.Log($"[PlayerSpawner] ✅ FindObjectsOfType에서 Yaw 가져옴: {data.cameraYaw:F1}");
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] ⚠️ Camera.main이 null입니다!");
        }

        Debug.Log($"[PlayerSpawner] 최종 cameraYaw 값: {data.cameraYaw:F1}");
        input.Set(data);
    }

    // 이하 콜백은 사용 안 함
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }
}
