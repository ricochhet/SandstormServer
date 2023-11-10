namespace Sandstorm.Core.Configuration.Models;

public class ConfigurationModel
{
    public int SpecifyModIOGameId { get; set; }
    public string SubscriptionObjectPath { get; set; }
    public string SandstormDataPath { get; set; }
    public string ModIOApiUrlBase { get; set; }
    public string LoggerOutputStreamPath { get; set; }
}
