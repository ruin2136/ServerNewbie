using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    public Button startButton; // 시작 버튼 (Inspector에서 연결)
    public NamePlate[] namePlates;

    private void Awake()
    {
        //이름표 초기화
        foreach (var plate in namePlates)
        {
            plate.SetPlate(false);
        }

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

    public void LobbyUIUpdate(List<string> names)
    {
        for (int i = 0; i < namePlates.Length; i++)
        {
            if (i < names.Count)
            {
                namePlates[i].SetPlate(true, names[i]); // 유효한 이름 설정
            }
            else
            {
                namePlates[i].SetPlate(false);          // 슬롯 초기화
            }
        }
    }
}
