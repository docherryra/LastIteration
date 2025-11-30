using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndSceneUI : MonoBehaviour
{
    [SerializeField] private Text endTitleText;
    [SerializeField] private Text reasonText;
    [SerializeField] private Text winnerText;
    [SerializeField] private Button goToMainButton;

    private void OnEnable()
    {
        UpdateLabels();
        InitializeCursor();
    }

    private void Start()
    {
        if (goToMainButton == null)
        {
            goToMainButton = GameObject.Find("GoToMainButton")?.GetComponent<Button>();
        }

        if (goToMainButton != null)
        {
            goToMainButton.onClick.AddListener(GoToMainScene);
        }
    }

    private void InitializeCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void UpdateLabels()
    {
        var (reason, winnerLine) = EndGameResult.GetRawMessage();

        if (endTitleText != null)
            endTitleText.text = "End Game!";

        if (reasonText != null)
            reasonText.text = string.IsNullOrEmpty(reason) ? "" : reason;

        if (winnerText != null)
            winnerText.text = string.IsNullOrEmpty(winnerLine) ? "Winner: N/A" : winnerLine;
    }

    public void GoToMainScene()
    {
        var networkHandler = FindObjectOfType<NetworkRunnerHandler>();
        if (networkHandler != null)
        {
            networkHandler.ShutdownNetwork();
        }
        InitializeCursor();
        SceneManager.LoadScene("menuScene");
    }
}