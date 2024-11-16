using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientPlayer : IPlayer
{
    // 인터페이스의 속성 구현
    public string name { get; set; }
    public bool onCoolTime { get; set; }
    public string answer { get; set; }

    public ClientPlayer(string nickname)
    {
        name = nickname;
        onCoolTime = false;
        answer = string.Empty;
    }

    //정답 서버에 제출
    public void SubmitAnswer()
    {

    }
    
    //정답 제출 시 쿨타임 부여
    public void BanSubmit()
    {

    }

    //현재 점수를 서버에서 가져와 출력
    public void PrintScore()
    {

    }
}
