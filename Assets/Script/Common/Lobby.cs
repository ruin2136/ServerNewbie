using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class Lobby : MonoBehaviour
{
    string lobbyId;                 //고유 로비 아이디
    List<GameClient> clients;       //클라이언트 목록
    int maxPlayer = 4;              //최대 정원
    
    public bool isFull = false;     //정원이 찼는지
    public bool isStart = false;    //게임 시작했는지
    //QuizManager quizManager;      //퀴즈 매니저

    public Lobby(string ID)
    {
        lobbyId = ID;
    }

    //로비에 플레이어 추가
    public void AddPlayer(GameClient client)
    {
        clients.Add(client);
        if(maxPlayer.Equals(clients.Count))
            isFull = true;
    }

    //로비에서 플레이어 제거
    public void RemovePlayer(GameClient client)
    {
        clients.Remove(client);
        if (!maxPlayer.Equals(clients.Count))
            isFull = false;
    }

    //모든 플레이어 준비상태 확인
    public void CheckReadyStatus()
    {

    }

    //게임 시작 및 씬 전환 신호 전송
    public void StartGame()
    {

    }

    //결과 출력
    public void ShowResult()
    {

    }
}
