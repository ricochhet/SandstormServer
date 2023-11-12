using System.Collections.Generic;

namespace Sandstorm.Core.Configuration.Models;

public class ConfigurationModel
{
    public int ModioGameId { get; set; }
    public string ModioApiKey { get; set; }
    public string ModioApiUrlBase { get; set; }
    public List<string> AddToSubscription { get; set; }
    public List<string> DoNotAddToSubscription { get; set; }
}
