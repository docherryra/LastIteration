using System.Threading.Tasks;
using Fusion.Photon.Realtime;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

// 네트워크 전체를 관리하는 매니저
public class NetworkRunnerHandler : MonoBehaviour
{
    private NetworkRunner _runner;
    private PlayerSpawner _spawner;
    private static NetworkRunnerHandler _instance;

    void Awake()
    {
        Debug.Log("[Fusion] NetworkRunnerHandler.Awake() 호출됨");

        // 싱글톤 패턴으로 중복 생성 방지
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[Fusion] NetworkRunnerHandler가 이미 존재합니다. 중복 생성 방지.");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 콜백용 플레이어 스포너 가져옴
        _spawner = GetComponent<PlayerSpawner>();

        // 기존 NetworkRunner 확인
        _runner = GetComponent<NetworkRunner>();

        // NetworkRunner가 없으면 생성
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            Debug.Log("[Fusion] NetworkRunner 생성 완료");
        }
        else
        {
            Debug.Log("[Fusion] 기존 NetworkRunner 사용");
        }

        // 입력을 Fusion으로 보내도록 설정
        _runner.ProvideInput = true;
    }

    async void Start()
    {
        Debug.Log("[Fusion] NetworkRunnerHandler.Start() 호출됨");

        // 인스턴스가 다르면 (중복 생성된 경우) Start 실행 안함
        if (_instance != this)
        {
            Debug.LogWarning("[Fusion] 중복 인스턴스의 Start() 실행 방지");
            return;
        }

        // Runner와 Spawner 둘 다 있어야 함
        if (_runner == null) {
            Debug.LogError("[Fusion] Runner 초기화 실패!");
            return;
        }
        if (_spawner == null) {
            Debug.LogError("[Fusion] PlayerSpawner 스크립트 없음!");
            return;
        }

        // 이미 실행 중이면 종료 후 재시작
        if (_runner.IsRunning)
        {
            Debug.LogWarning("[Fusion] Runner가 이미 실행 중입니다. 종료 후 재시작합니다.");
            await _runner.Shutdown();
            await Task.Delay(500); // 완전히 종료될 때까지 대기
        }

        // 콜백 등록
        _runner.AddCallbacks(_spawner);
        Debug.Log("[Fusion] PlayerSpawner Callback 등록 완료");

        // 현재 씬을 네트워크 씬으로 등록
        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var appSettings = PhotonAppSettings.Global.AppSettings;

        // 실행 모드 자동 결정
        GameMode mode;

#if UNITY_EDITOR
        mode = GameMode.Host;     // ✦ 에디터에서는 무조건 Host
        Debug.Log("▶ Editor 실행 → Host(서버) 모드");
#else
        mode = GameMode.Client;   // ✦ 빌드된 앱에서는 Client
        Debug.Log("▶ Build 실행 → Client 모드");
#endif

        // NetworkSceneManagerDefault가 이미 있는지 확인
        var sceneManager = GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null)
        {
            sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        // 게임 실행 설정
        var startArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = sceneRef,
            SceneManager = sceneManager,
            CustomPhotonAppSettings = appSettings
        };

        // 세션 시작
        var result = await _runner.StartGame(startArgs);

        // 세션 상태 출력
        if (result.Ok) {
            Debug.Log($"✅ 세션 접속 성공: {startArgs.SessionName} | Mode: {_runner.GameMode}");
        } else {
            Debug.LogError($"❌ 세션 시작 실패: {result.ShutdownReason}");
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
