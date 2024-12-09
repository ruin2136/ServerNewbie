using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class Lobby
{
    public string lobbyId;          //고유 로비 아이디
    public List<ServerClient> clients=new() { };       //클라이언트 목록
    int maxPlayer = 4;              //최대 정원
    
    public bool isFull = false;     //정원이 찼는지
    public bool isStart = false;    //게임 시작했는지
    //QuizManager quizManager;      //퀴즈 매니저

    public Lobby(string ID)
    {
        lobbyId = ID;
    }

    // 클라이언트 추가
    public void AddPlayer(ServerClient client)
    {
        if (!isFull)
        {
            clients.Add(client);
            Debug.Log($"플레이어가 {lobbyId} 로비에 추가되었습니다.");

            if(clients.Count.Equals(maxPlayer))
                isFull = true;
        }
        else
        {
            Debug.LogError("로비가 꽉 찼습니다.");
        }
    }

    //로비에서 플레이어 제거
    public void RemovePlayer(ServerClient client)
    {
        clients.Remove(client);
        if (!clients.Count.Equals(maxPlayer))
            isFull = false;
        Debug.Log($"플레이어 {client}가 {lobbyId} 로비에서 제거되었습니다.");
    }
}
