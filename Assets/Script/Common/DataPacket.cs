using System.Text;
using Newtonsoft.Json;

public class DataPacket
{
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
