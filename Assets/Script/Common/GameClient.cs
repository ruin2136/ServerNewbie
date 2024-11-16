using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameClient : MonoBehaviour
{
    string serverAddress;   //서버 IP 주소
    int serverPort;         //서버 포트 넘버
    //sockeet               //소켓(미완성)
    private static GameClient instance = null;
    public bool isReady = false;    //준비됐는지

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
    public static GameClient Instance
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

    //서버와 연결
    public void Connect()
    {

    }

    //서버와 연결 종료
    public void Disconnect()
    {

    }

    //서버로부터 데이터 수신
    public void SendData(string data)
    {

    }

    //서버로부터 데이터 송신
    public void ReceiveData()
    {

    }

    //클라 접속 시 로비에 접근 신호 전송 메서드
    public void JointLobby()
    {

    }
    
    //로비 준비버튼
    public void SetReady()
    {

    }
}
