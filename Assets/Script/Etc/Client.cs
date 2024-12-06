using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Text;
using System;

public class Client : MonoBehaviour
{
    public InputField IPInput, PortInput, NickInput;  // NickInput을 UI InputField로 사용
    string clientName;

    bool socketReady;
    TcpClient socket;
    NetworkStream stream;

    public void ConnectToServer()
    {
        // 이미 연결되었다면 무시
        if (socketReady) return;

        // 기본 호스트/포트번호
        string ip = IPInput.text == "" ? "127.0.0.1" : IPInput.text;
        int port = PortInput.text == "" ? 7777 : int.Parse(PortInput.text);

        // 소켓 생성
        try
        {
            socket = new TcpClient(ip, port);
            stream = socket.GetStream();
            socketReady = true;

            // 클라이언트 닉네임 확인
            clientName = NickInput.text == "" ? "Guest" + UnityEngine.Random.Range(1000, 10000) : NickInput.text;
            Send($"&NAME|{clientName}");  // 닉네임 전송
        }
        catch (Exception e)
        {
            Chat.instance.ShowMessage($"소켓 에러: {e.Message}");
        }
    }

    void Update()
    {
        if (socketReady && stream.DataAvailable)
        {
            // 데이터 읽기
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead > 0)
            {
                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                OnIncomingData(data);
            }
        }
    }

    void OnIncomingData(string data)
    {
        Debug.Log("서버에서 받은 데이터: " + data);  // 데이터 출력 (디버깅)

        // DataPacket 역직렬화
        DataPacket packet = DataPacket.Deserialize(Encoding.UTF8.GetBytes(data));

        if (packet.Type == "message")
        {
            Chat.instance.ShowMessage(packet.Value);  // 채팅 메시지 출력
        }
    }

    void Send(string data)
    {
        if (!socketReady) return;

        try
        {
            // DataPacket 객체로 감싸서 전송
            DataPacket packet = new DataPacket("message", data);  // 메시지를 DataPacket 객체로 감싼다
            byte[] serializedData = packet.Serialize();  // 직렬화하여 바이트 배열로 변환
            stream.Write(serializedData, 0, serializedData.Length);
            Debug.Log("서버로 전송한 데이터: " + data);  // 데이터 출력 (디버깅)
        }
        catch (Exception e)
        {
            Chat.instance.ShowMessage($"데이터 전송 에러: {e.Message}");
        }
    }

    public void OnSendButton(InputField SendInput)
    {
#if (UNITY_EDITOR || UNITY_STANDALONE)
        if (!Input.GetButtonDown("Submit")) return;
        SendInput.ActivateInputField();
#endif
        if (SendInput.text.Trim() == "") return;

        string message = SendInput.text;
        SendInput.text = "";
        Send(message);  // 서버로 메시지 전송
    }

    void OnApplicationQuit()
    {
        CloseSocket();
    }

    void CloseSocket()
    {
        if (!socketReady) return;

        try
        {
            stream.Close();
            socket.Close();
            socketReady = false;
        }
        catch (Exception e)
        {
            Chat.instance.ShowMessage($"소켓 닫기 에러: {e.Message}");
        }
    }
}
