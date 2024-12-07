using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    public Button readyBtn;         // 준비 버튼
    public Text btnText;            // 준비 버튼 Text
    public NamePlate[] namePlates;  // 플레이어 이름표 배열

    private void Awake()
    {
        //버튼 초기화
        SetBtn(false);

        //이름표 초기화
        foreach (var plate in namePlates)
        {
            plate.SetPlate(false);
        }

        //InitializeStartButton();
    }

    private void InitializeStartButton()
    {
        if (Server.Instance != null && readyBtn != null)
        {
            readyBtn.onClick.RemoveAllListeners(); // 이전 이벤트 제거
            readyBtn.onClick.AddListener(Server.Instance.OnBroadcastQuizButton);
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

    public void SetBtn(bool active)
    {
        Debug.Log("준비 버튼 상태 변경");

        //버튼 활성화
        if(active)
        {
            readyBtn.interactable = true;
            readyBtn.image.color = new Color(255,255,255, 255);
            btnText.text = "눌러서 준비 완료";
        }
        //버튼 비활성화
        else
        {
            readyBtn.interactable = false;
            readyBtn.image.color = new Color(110,110,110,255);
            btnText.text = "플레이어 대기 중";
        }
    }

    public void ReadyBtnClick()
    {
        Client.Instance.SetReady();

        //준비 되었으면
        if(Client.Instance.isReady)
        {
            readyBtn.image.color= new Color(175, 255, 150, 255);
            btnText.text = "준비 완료!";
        }
        //준비 안됐으면
        else
        {
            readyBtn.image.color = new Color(255, 255, 255, 255);
            btnText.text = "눌러서 준비 완료";
        }
    }
}
