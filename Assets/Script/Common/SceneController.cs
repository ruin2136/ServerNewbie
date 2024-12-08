using System.Collections.Generic;
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
        if (scene.name == "LobbyScene")
        {
            // 로비 씬에 초기화 및 업데이트
            InitializeLobbyScene();
        }
        else if(scene.name == "QuizScene")
        {
            // 퀴즈 씬 초기화
            InitializeQuizScene();
        }
        else if(scene.name == "ResultScene")
        {
            // 결과 씬 초기화
            InitializeResultScene();
        }
    }

    private void InitializeLobbyScene()
    {
        //Client에 로비 UI 매니저 할당
        GameObject lobObject = GameObject.Find("LobbyUIManager");
        Debug.Log(lobObject);
        if (lobObject != null)
        {
            LobbyUIManager lobUI = lobObject.GetComponent<LobbyUIManager>();
            if (lobUI != null)
            {
                Client.Instance.lobManager = lobUI;

                //로비 UI 갱신 호출
                lobUI.LobbyUIUpdate(Client.Instance.playerNames);

                Debug.Log($"신규 접속 플레이어 : 버튼 상태 업데이트 to {Client.Instance.readyBtnSet}");
                //로비 준비버튼 갱신 호출
                lobUI.SetBtn(Client.Instance.readyBtnSet);
            }
            else
            {
                Debug.LogError($"{lobObject.name} 오브젝트에 LobbyUIManager 컴포넌트가 없습니다.");
            }
        }
        else
        {
            Debug.LogError("LobbyUIManager 오브젝트를 찾을 수 없습니다.");
        }

        //Chat 오브젝트 할당
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

    private void InitializeQuizScene()
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

    private void InitializeResultScene()
    {
        GameObject rankObject = GameObject.Find("Rank");
        if (rankObject != null)
        {
            Rank rankComponent = rankObject.GetComponent<Rank>();
            rankComponent.DisplayRankings(Client.Instance.playerScores, Client.Instance.playerNames);
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
