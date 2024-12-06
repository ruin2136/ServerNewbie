using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using UnityEngine;

public class DataPacket
{
    public string Type { get; set; }
    public string Value { get; set; }

    // 생성자
    public DataPacket(string type, string value)
    {
        Type = type;
        Value = value;
    }

    #region 직렬화/역직렬화 (by 김재민)
    // 바이트 배열로 직렬화
    public byte[] Serialize()
    {
        // 문자열을 UTF-8 바이트 배열로 변환 후 길이를 포함한 배열을 생성
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(memoryStream))
            {
                // Type 길이와 Type 데이터
                writer.Write(Type.Length);
                writer.Write(Encoding.UTF8.GetBytes(Type));

                // Value 길이와 Value 데이터
                writer.Write(Value.Length);
                writer.Write(Encoding.UTF8.GetBytes(Value));

                // 직렬화된 데이터 로그로 출력
                Debug.Log($"Serialized packet: Type = {Type}, Value = {Value}");
                Debug.Log($"Serialized data (byte array): {BitConverter.ToString(memoryStream.ToArray())}");
            }
            return memoryStream.ToArray();
        }
    }

    // 바이트 배열로부터 역직렬화
    public static DataPacket Deserialize(byte[] data)
    {
        using (MemoryStream memoryStream = new MemoryStream(data))
        {
            using (BinaryReader reader = new BinaryReader(memoryStream))
            {
                // Type 읽기
                int typeLength = reader.ReadInt32();
                string type = Encoding.UTF8.GetString(reader.ReadBytes(typeLength));

                // Value 읽기
                int valueLength = reader.ReadInt32();
                string value = Encoding.UTF8.GetString(reader.ReadBytes(valueLength));

                // 역직렬화된 데이터 로그로 출력
                Debug.Log($"Deserialized packet: Type = {type}, Value = {value}");
                return new DataPacket(type, value);
            }
        }
    }
    #endregion


    #region 직렬화/역직렬화 (by 전영준)
    // 직렬화 메서드
    public byte[] Serialize_2()
    {
        // 객체를 JSON 문자열로 변환
        string jsonString = JsonConvert.SerializeObject(this);
        // JSON 문자열을 바이트 배열로 변환
        return Encoding.UTF8.GetBytes(jsonString);
    }

    // 역직렬화 메서드
    public static DataPacket Deserialize_2(byte[] data)
    {
        // 바이트 배열을 JSON 문자열로 변환
        string jsonString = Encoding.UTF8.GetString(data);
        // JSON 문자열을 DataPacket 객체로 변환
        return JsonConvert.DeserializeObject<DataPacket>(jsonString);
    }
    #endregion

}
