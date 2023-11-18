using System.Collections.Generic;

namespace Sandstorm.Core.Models;

public class SettingsModel
{
    public int GameId { get; set; }
    public string ApiKey { get; set; }
    public string ApiUrlBase { get; set; }
    public List<string> AddToSubscription { get; set; }
    public List<string> DoNotAddToSubscription { get; set; }
}
