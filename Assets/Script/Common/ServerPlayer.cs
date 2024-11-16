using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayer : IPlayer
{
    // 인터페이스의 속성 구현
    public string name { get; set; }
    public bool onCoolTime { get; set; }
    public string answer { get; set; }
    
    public int score {  get; set; }     //플레이어 점수

    public ServerPlayer(string nickname)
    {
        name = nickname;
        onCoolTime = false;
        answer = string.Empty;
    }

    //점수를 가져오는 메서드
    public void GetScore()
    {

    }
}
