using System.Text;
using Newtonsoft.Json;

public class DataPacket
{
    //NetworkStream 사용을 위해 제작됨
    //StreamWriter, StreamReader는 string 타입만 송수신 가능
    //DataPacket이란 자료형을 전송하기 위해선 마찬가지로 json으로 변환 필요
    //그래서 그냥 과제에서도 쓰이는 NetworkStream을 사용
    //이에 맞춰 바이트 배열로 직렬화, 역직렬화

    public string Type { get; set; }    // 데이터 타입
    public string Value { get; set; }   // 데이터 값

    public DataPacket(string type, string value)
    {
        Type = type;
        Value = value;
    }

    // 직렬화 메서드
    public byte[] Serialize()
    {
        // 객체를 JSON 문자열로 변환
        string jsonString = JsonConvert.SerializeObject(this);
        // JSON 문자열을 바이트 배열로 변환
        return Encoding.UTF8.GetBytes(jsonString);
    }

    // 역직렬화 메서드
    public static DataPacket Deserialize(byte[] data)
    {
        // 바이트 배열을 JSON 문자열로 변환
        string jsonString = Encoding.UTF8.GetString(data);
        // JSON 문자열을 DataPacket 객체로 변환
        return JsonConvert.DeserializeObject<DataPacket>(jsonString);
    }
}
