using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 이동 시에도 유지
        }
        else
        {
            Destroy(gameObject); // 중복된 인스턴스 제거
        }
    }

    // 씬 이동 메서드
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        
    }

    // 씬 로드 후 초기화 작업
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"씬 로드 완료: {scene.name}");
        if (scene.name == "LobbyScene" || scene.name=="QuizScene" ) // 로비 씬에서만 특정 초기화 진행
        {
            InitializeLobbyScene();
            OnDisable();
        }
    }

    private void InitializeLobbyScene()
    {
        GameObject chatObject = GameObject.Find("Chat");
        Debug.Log(chatObject);
        if (chatObject != null)
        {
            Chat chatComponent = chatObject.GetComponent<Chat>();
            if (chatComponent != null)
            {
                Chat.instance = chatComponent;
            }
            else
            {
                Debug.LogError("Chat 오브젝트에 Chat 컴포넌트가 없습니다.");
            }
        }
        else
        {
            Debug.LogError("Chat 오브젝트를 찾을 수 없습니다.");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // 씬 로드 이벤트 구독
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // 씬 로드 이벤트 구독 해제
    }
}
