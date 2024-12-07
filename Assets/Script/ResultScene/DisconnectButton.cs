using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisconnectButton : MonoBehaviour
{
    //시작 씬으로 이동
    public void MoveToStart()
    {
        Client.Instance.CloseSocket();
        SceneController.Instance.LoadScene("StartScene");
    }

    //게임 종료
    public void QuitGame()
    {
        Application.Quit();
    }
 
}
