using System.Text.Json;
using Sandstorm.Core.Providers;

namespace Sandstorm.Core.Helpers;

public static class JsonHelper
{
    public static T Read<T>(string pathToFile)
    {
        return JsonSerializer.Deserialize<T>(FsProvider.ReadAllText(pathToFile));
    }

    public static void Write(string folderPath, string fileName, object data, JsonSerializerOptions options = null)
    {
        FsProvider.WriteFile(folderPath, fileName, JsonSerializer.Serialize(data, options));
    }
}
