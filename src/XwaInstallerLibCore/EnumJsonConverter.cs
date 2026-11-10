using Newtonsoft.Json;

namespace XwaInstallerLibCore;

public sealed class EnumJsonConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return (T)Enum.Parse(objectType, reader.Value!.ToString()!);
    }

    public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}
