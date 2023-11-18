using System.Text.Json;

namespace Sandstorm.Core.MiniCommon;

public static class Json
{
    public static T Read<T>(string pathToFile)
    {
        return JsonSerializer.Deserialize<T>(FsProvider.ReadAllText(pathToFile));
    }

    public static void Write(string folderPath, string fileName, object data, JsonSerializerOptions options = null)
    {
        FsProvider.WriteFile(folderPath, fileName, JsonSerializer.Serialize(data, options));
    }

    public static void Write(string folderPath, string fileName, string data, JsonSerializerOptions options = null)
    {
        object deserialized = JsonSerializer.Deserialize<object>(data);
        FsProvider.WriteFile(folderPath, fileName, JsonSerializer.Serialize(deserialized, options));
    }
}
