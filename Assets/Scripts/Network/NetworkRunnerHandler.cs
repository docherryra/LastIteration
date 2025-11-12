using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;


// NetworkRunnerHandler에 붙어 네트워크 관리
// 서버 연결, 룸 생성, 씬 관리
public class NetworkRunnerHandler : MonoBehaviour
{
    private NetworkRunner _runner;

    async void Start()
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient, // 방 없으면 Host, 있으면 Client로 실행
            SessionName = "TestRoom",
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), // 현재 씬을 네트워크 씬으로 등록
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        await _runner.StartGame(startGameArgs);
    }
}
