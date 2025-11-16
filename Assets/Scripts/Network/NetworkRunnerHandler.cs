using System.Threading.Tasks;
using Fusion.Photon.Realtime;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;


// NetworkRunnerHandler에 붙어 네트워크 관리
// 서버 연결, 룸 생성, 씬 관리
public class NetworkRunnerHandler : MonoBehaviour
{
    private NetworkRunner _runner;
    private PlayerSpawner _spawner;

    async void Awake()
    {
        if (FindObjectsOfType<NetworkRunnerHandler>().Length > 1) {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    
        // 콜백 객체 미리 찾음
        _spawner = GetComponent<PlayerSpawner>();
    }


    async void Start()
    {
        Debug.Log("[Fusion] NetworkRunnerHandler.Start() 호출됨");

        // Runner 생성
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // Runner 준비 완료 대기
        await Task.Yield(); // 한 프레임 대기 (Runner 초기화 보장)
        
        // spawner 초기화
        if (_spawner != null) {
            _runner.AddCallbacks(_spawner);
            Debug.Log("[Fusion] PlayerSpawner callback registered 콜백 성공");
        } else {
            Debug.LogError("[Fusion] PlayerSpawner not found ❌");
        }

        // 씬 정보
        var activeScene = SceneManager.GetActiveScene();
        var sceneRef = SceneRef.FromIndex(activeScene.buildIndex);

        var photonAppSettings = PhotonAppSettings.Global;

        // 게임 시작 정보 설정
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient, // 방 없으면 Host, 있으면 Client로 실행
            SessionName = "TestRoom",
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), // 현재 씬을 네트워크 씬으로 등록
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            CustomPhotonAppSettings = photonAppSettings.AppSettings
        };

        // 세션 시작
        var result = await _runner.StartGame(startGameArgs);

        // 세션 상태 디버깅
        if (result.Ok)
        {
            Debug.Log($"✅ !! Joined session: {startGameArgs.SessionName} | Mode: {_runner.GameMode}");
        }
        else
        {
            Debug.LogError($"❌ !! Failed to start: {result.ShutdownReason}");
        }
    }
}
