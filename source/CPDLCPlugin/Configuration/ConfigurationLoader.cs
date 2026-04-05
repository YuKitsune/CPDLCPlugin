using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using vatsys;

namespace CPDLCPlugin.Configuration;

public static class ConfigurationLoader
{
    const string ConfigFileName = "CPDLC.json";

    public static PluginConfiguration Load()
    {
        var searchDirectories = new List<string>();

        // Search the profile first
        if (TryFindProfileDirectory(out var profileDirectory))
        {
            searchDirectories.AddRange([
                Path.Combine(profileDirectory.FullName, "Plugins", "Configs", "CPDLC Plugin"),
                Path.Combine(profileDirectory.FullName, "Plugins", "Configs", "CPDLCPlugin"),
                Path.Combine(profileDirectory.FullName, "Plugins", "Configs", "CPDLC"),
                Path.Combine(profileDirectory.FullName, "Plugins", "Configs"),
                Path.Combine(profileDirectory.FullName, "Plugins"),
                profileDirectory.FullName
            ]);
        }

        // Search the assembly directory last
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        searchDirectories.Add(assemblyDirectory);

        var configFilePath = string.Empty;
        foreach (var searchDirectory in searchDirectories)
        {
            var filePath = Path.Combine(searchDirectory, ConfigFileName);
            if (!File.Exists(filePath))
                continue;

            configFilePath = filePath;
            break;
        }

        if (string.IsNullOrEmpty(configFilePath))
            throw new FileNotFoundException($"Unable to locate {ConfigFileName}");

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

    // Thanks Max!
    static bool TryFindProfileDirectory(out DirectoryInfo? directoryInfo)
    {
        directoryInfo = null;
        if (!Profile.Loaded)
            return false;

        var shortNameObject = typeof(Profile).GetField("shortName", BindingFlags.Static | BindingFlags.NonPublic);
        var shortName = (string)shortNameObject.GetValue(shortNameObject);

        directoryInfo = new DirectoryInfo(Path.Combine(Helpers.GetFilesFolder(), "Profiles", shortName));
        return true;
    }
}
