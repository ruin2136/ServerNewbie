using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameServer : MonoBehaviour
{
    List<Lobby> lobbies = new List<Lobby>();

    //임시 변수
    GameClient client;

    private static GameServer instance = null;

    private void Awake()
    {
        #region 싱글톤 처리
        if (null == instance)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
        #endregion
    }

    #region 싱글톤 프로퍼티
    public static GameServer Instance
    {
        get
        {
            if (null == instance)
            {
                return null;
            }
            return instance;
        }
    }
    #endregion

    //최초 클라이언트 접속 처리
    public void ClientJoin(GameClient client)
    {
        foreach (var lob in lobbies)
        {
            if (!lob.isFull)
            {
                ConnectLobby(lob, client);
                return;
            }
        }
            
        ConnectLobby(CreateLobby(), client);

    }

    //로비 생성
    public Lobby CreateLobby()
    {
        Lobby newLob = new Lobby(Guid.NewGuid().ToString());    //고유 아이디 부여
        lobbies.Add(newLob);

        return newLob;
    }

    //로비와 연결
    public void ConnectLobby(Lobby lobby, GameClient client)
    {
        if (lobby.isFull)
        {
            Debug.LogError("로비 에러 : 이미 차있는 로비에 접속을 시도했습니다.");
            return;
        }

        lobby.AddPlayer(client);
    }

    //로비 제거
    public void RemoveLobby()
    {

    }

    //로비 목록 호출
    public void GetLobbyList()
    {

    }
}
