using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;

public class TCP_Server : MonoBehaviour
{
    public InputField PortInput;

    List<ServerClient> clients;
    List<ServerClient> disconnectList;

    TcpListener server;
    bool serverStarted;

    public void ServerCreate()
    {
        clients = new List<ServerClient>();
        disconnectList = new List<ServerClient>();

        try
        {
            int port = PortInput.text == "" ? 7777 : int.Parse(PortInput.text);
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            StartListening();
            serverStarted = true;
            Chat.instance.ShowMessage($"서버가 {port}에서 시작되었습니다.");
            Debug.Log($"서버가 {port}에서 시작되었습니다.");
        }
        catch (Exception e)
        {
            Chat.instance.ShowMessage($"소켓 에러: {e.Message}");
        }
    }

    void Update()
    {
        if (!serverStarted) return;

        foreach (ServerClient c in clients)
        {
            // 클라이언트가 여전히 연결되어 있는지 확인
            if (!IsConnected(c.tcp))
            {
                c.tcp.Close();
                disconnectList.Add(c);
                continue;
            }

            // 클라이언트로부터 데이터 수신
            else
            {
                NetworkStream s = c.tcp.GetStream();
                if (s.DataAvailable)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = s.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        OnIncomingData(c, data);
                    }
                }
            }
        }

        // 연결이 끊어진 클라이언트 처리
        for (int i = 0; i < disconnectList.Count; i++)
        {
            Broadcast($"{disconnectList[i].clientName} 연결이 끊어졌습니다", clients);
            clients.Remove(disconnectList[i]);
            disconnectList.RemoveAt(i);
        }
    }

    bool IsConnected(TcpClient c)
    {
        try
        {
            if (c != null && c.Client != null && c.Client.Connected)
            {
                if (c.Client.Poll(0, SelectMode.SelectRead))
                    return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);

                return true;
            }
            else
                return false;
        }
        catch
        {
            return false;
        }
    }

    void StartListening()
    {
        server.BeginAcceptTcpClient(AcceptTcpClient, server);
    }

    void AcceptTcpClient(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        ServerClient newClient = new ServerClient(listener.EndAcceptTcpClient(ar));
        clients.Add(newClient);
        StartListening();
    }

    void OnIncomingData(ServerClient c, string data)
    {
        Debug.Log("서버에서 받은 데이터: " + data);  // 서버에서 받은 데이터 출력 (디버깅)

        // DataPacket 역직렬화
        DataPacket packet = DataPacket.TCPDeserialize(Encoding.UTF8.GetBytes(data));
        
        if (packet.Type == "message")
        {
            // 클라이언트가 보내는 메시지 처리
            Broadcast($"{c.clientName} : {packet.Value}", clients);  // 채팅 메시지 처리
        }
    }

    void Broadcast(string data, List<ServerClient> cl)
    {
        DataPacket packet = new DataPacket("message", data);  // 메시지를 DataPacket 객체로 감싼다
        byte[] serializedData = packet.TCPSerialize();  // 직렬화하여 바이트 배열로 변환
        
        foreach (var c in cl)
        {
            try
            {
                NetworkStream stream = c.tcp.GetStream();
                stream.Write(serializedData, 0, serializedData.Length);
                Debug.Log("서버에서 클라이언트로 보낸 데이터: " + data);  // 서버에서 보낸 데이터 출력 (디버깅)
            }
            catch (Exception e)
            {
                Chat.instance.ShowMessage($"쓰기 에러: {e.Message} - 클라이언트 {c.clientName}");
            }
        }
    }
}
