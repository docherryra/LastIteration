using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// 간단한 전투/시스템 로그 표시
public class CombatLogUI : MonoBehaviour
{
    [SerializeField] private TMP_Text logText;
    [SerializeField] private int maxLines = 8;
    [SerializeField] private float entryLifetime = 30f; // 각 로그 유지 시간(초)

    private class Entry
    {
        public string text;
        public float expireTime;
    }

    private readonly Queue<Entry> lines = new Queue<Entry>();
    private bool dirty = false;

    public static CombatLogUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        bool removed = false;

        while (lines.Count > 0 && lines.Peek().expireTime <= now)
        {
            lines.Dequeue();
            removed = true;
        }

        if (removed)
        {
            Refresh();
        }
    }

    public void AddMessage(string message, Color color)
    {
        string colored = $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{message}</color>";
        lines.Enqueue(new Entry { text = colored, expireTime = Time.unscaledTime + entryLifetime });
        while (lines.Count > maxLines)
            lines.Dequeue();

        dirty = true;
        Refresh();
    }

    private void Refresh()
    {
        if (!dirty)
            return;

        if (logText != null)
        {
            List<string> buffer = new List<string>(lines.Count);
            foreach (var e in lines)
                buffer.Add(e.text);

            logText.text = string.Join("\n", buffer);
        }
        else
        {
            // logText가 없으면 콘솔에 남김
            if (lines.Count > 0)
                Debug.Log($"[CombatLogUI] {lines.Peek().text}");
        }

        dirty = false;
    }
}
