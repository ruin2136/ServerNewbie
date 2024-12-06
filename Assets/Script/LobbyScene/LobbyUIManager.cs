using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    public Button startButton; // 시작 버튼 (Inspector에서 연결)
    
    private void Start()
    {
        InitializeStartButton();
    }

    private void InitializeStartButton()
    {
        if (Server.Instance != null && startButton != null)
        {
            startButton.onClick.RemoveAllListeners(); // 이전 이벤트 제거
            startButton.onClick.AddListener(Server.Instance.OnBroadcastQuizButton);
        }
        else
        {
            Debug.LogError("서버 인스턴스나 시작 버튼을 찾을 수 없습니다.");
        }
    }
}
