public interface IPlayer
{
    string name { get; set; }       //닉네임
    bool onCoolTime {  get; set; }  //쿨타임 여부
    string answer { get; set; }     //제출한 정답

}
