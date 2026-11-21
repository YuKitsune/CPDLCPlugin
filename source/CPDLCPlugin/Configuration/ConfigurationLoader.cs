using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using vatSysToolbox.Configuration;

namespace CPDLCPlugin.Configuration;

public static class ConfigurationLoader
{
    const string ConfigFileName = "CPDLC.json";

    public static PluginConfiguration Load()
    {
        var configFilePath = ConfigurationLocator.LocateConfigurationFile(
            typeof(Plugin).Assembly,
            ConfigFileName,
            "CPDLC Plugin",
            "CPDLCPlugin",
            "CPDLC");

        var configurationJson = File.ReadAllText(configFilePath);
        var configuration = JsonSerializer.Deserialize<PluginConfiguration>(configurationJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter() },
            AllowTrailingCommas = true
        })!;

        return configuration;
    }
}
