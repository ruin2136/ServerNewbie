using System.Text;
using Newtonsoft.Json;

public class DataPacket
{
    public string Type { get; set; }    // ������ Ÿ��
    public string Value { get; set; }   // ������ ��

    public DataPacket(string type, string value)
    {
        Type = type;
        Value = value;
    }

    // ����ȭ �޼���
    public byte[] Serialize()
    {
        // ��ü�� JSON ���ڿ��� ��ȯ
        string jsonString = JsonConvert.SerializeObject(this);
        // JSON ���ڿ��� ����Ʈ �迭�� ��ȯ
        return Encoding.UTF8.GetBytes(jsonString);
    }

    // ������ȭ �޼���
    public static DataPacket Deserialize(byte[] data)
    {
        // ����Ʈ �迭�� JSON ���ڿ��� ��ȯ
        string jsonString = Encoding.UTF8.GetString(data);
        // JSON ���ڿ��� DataPacket ��ü�� ��ȯ
        return JsonConvert.DeserializeObject<DataPacket>(jsonString);
    }
}
