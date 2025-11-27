using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Respawn Spawn Points (8개)")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnCooldownSeconds = 5f;

    // 다른 스크립트(예: PlayerState)에서 쉽게 접근하기 위한 싱글톤
    public static Spawner Instance { get; private set; }

    private float[] spawnCooldownEndTimes;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Spawner] spawnPoints가 비어 있습니다. 인스펙터에서 설정하세요.");
        }
        else
        {
            spawnCooldownEndTimes = new float[spawnPoints.Length];
            for (int i = 0; i < spawnCooldownEndTimes.Length; i++)
                spawnCooldownEndTimes[i] = float.NegativeInfinity;
        }
    }

    /// <summary>
    /// 8개의 스폰 포인트 중 하나를 랜덤으로 반환
    /// </summary>
    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Spawner] spawnPoints가 비어 있습니다. 현재 위치로 반환합니다.");
            return transform; // null 반환 대신 자기 Transform
        }

        float now = Time.time;
        var availableIndices = new System.Collections.Generic.List<int>();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
                continue;

            if (now >= spawnCooldownEndTimes[i])
                availableIndices.Add(i);
        }

        int chosenIndex;
        if (availableIndices.Count > 0)
        {
            chosenIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        }
        else
        {
            // 모든 스폰 포인트가 쿨타임이면, 가장 빨리 열리는 지점을 사용
            float earliestTime = float.PositiveInfinity;
            int earliestIndex = 0;
            for (int i = 0; i < spawnCooldownEndTimes.Length; i++)
            {
                if (spawnPoints[i] == null)
                    continue;

                if (spawnCooldownEndTimes[i] < earliestTime)
                {
                    earliestTime = spawnCooldownEndTimes[i];
                    earliestIndex = i;
                }
            }
            chosenIndex = earliestIndex;
            Debug.LogWarning("[Spawner] 모든 스폰 포인트가 쿨타임입니다. 가장 빨리 열리는 지점을 사용합니다.");
        }

        spawnCooldownEndTimes[chosenIndex] = now + spawnCooldownSeconds;
        Debug.Log($"[Spawner] Respawn spawn index={chosenIndex} (cooldown {spawnCooldownSeconds}s)");
        return spawnPoints[chosenIndex];
    }

    /// <summary>
    /// position 값만 필요할 때
    /// </summary>
    public Vector3 GetRandomSpawnPosition()
    {
        Transform t = GetRandomSpawnPoint();
        return t != null ? t.position : Vector3.zero;
    }
}
