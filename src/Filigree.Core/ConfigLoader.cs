using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Filigree.Core;

public static class ConfigLoader
{
    /// <summary>Loads YAML config, or returns defaults if the file is absent.</summary>
    public static FiligreeConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"[filigree] no config at {path}, using defaults");
            return new FiligreeConfig();
        }

        try
        {
            var yaml = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return yaml.Deserialize<FiligreeConfig>(File.ReadAllText(path)) ?? new FiligreeConfig();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[filigree] config parse failed ({ex.Message}), using defaults");
            return new FiligreeConfig();
        }
    }
}
