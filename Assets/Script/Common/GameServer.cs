using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class GameServer : MonoBehaviour
{
    List<Lobby> lobbies = new List<Lobby>();

    //임시 변수
    GameClient client;

    private static GameServer instance = null;

    // 세마포 선언 (하나의 로비만 생성/삭제하도록 보장)
    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1); // 한 번에 하나만 실행

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

    // 최초 클라이언트 접속 처리
    public async Task ClientJoinAsync(GameClient client)
    {
        foreach (var lob in lobbies)
        {
            if (!lob.isFull)
            {
                await ConnectLobbyAsync(lob, client);
                return;
            }
        }

        await ConnectLobbyAsync(await CreateLobbyAsync(), client);
    }

    // 비동기 로비 생성
    public async Task<Lobby> CreateLobbyAsync()
    {
        await semaphore.WaitAsync(); // 세마포로 락 획득

        try
        {
            Lobby newLob = new Lobby(Guid.NewGuid().ToString());  // 고유 아이디 부여
            lobbies.Add(newLob);
            return newLob;
        }
        finally
        {
            semaphore.Release(); // 락 해제
        }
    }

    // 비동기 로비 삭제
    public async Task RemoveLobbyAsync(Lobby lobby)
    {
        await semaphore.WaitAsync(); // 세마포로 락 획득

        try
        {
            if (lobbies.Contains(lobby))
            {
                lobbies.Remove(lobby);
                Debug.Log($"로비 {lobby.lobbyId} 삭제 완료.");
            }
            else
            {
                Debug.LogError("로비가 존재하지 않습니다.");
            }
        }
        finally
        {
            semaphore.Release(); // 락 해제
        }
    }

    // 비동기 로비와 클라이언트 연결
    public async Task ConnectLobbyAsync(Lobby lobby, GameClient client)
    {
        if (lobby.isFull)
        {
            Debug.LogError("로비 에러 : 이미 차있는 로비에 접속을 시도했습니다.");
            return;
        }

        await lobby.AddPlayerAsync(client);
    }

    // 로비 목록 호출 (비동기 처리 가능)
    public async Task GetLobbyListAsync()
    {
        await Task.Yield(); // 비동기로 실행 (여기서는 예시로 사용)
        foreach (var lobby in lobbies)
        {
            Debug.Log($"로비 ID: {lobby.lobbyId}, 상태: {(lobby.isFull ? "Full" : "Available")}");
        }
    }
}
