using System.IO;
using vatSysToolbox.Configuration;
using YamlDotNet.Serialization;

namespace CPDLCPlugin.Configuration;

public static class ConfigurationLoader
{
    const string ConfigFileName = "CPDLC.yaml";

    public static PluginConfiguration Load()
    {
        var configFilePath = ConfigurationLocator.LocateConfigurationFile(
            typeof(Plugin).Assembly,
            ConfigFileName,
            "CPDLC Plugin",
            "CPDLCPlugin",
            "CPDLC");

        // The keys in the file are the same as the property names, for example "ServerEndpoint"
        var configurationYaml = File.ReadAllText(configFilePath);
        var deserializer = new DeserializerBuilder().Build();
        var configuration = deserializer.Deserialize<PluginConfiguration>(configurationYaml);

        return configuration;
    }
}
