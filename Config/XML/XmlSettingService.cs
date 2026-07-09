using System.Collections.Concurrent;
using System.Xml.Serialization;
using TableBackend.Config.XML.Email;

namespace TableBackend.Config.XML;

public static class XmlSettingService
{
    private static string BaseDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory + "\\config";
    private static bool IsRead { get; set; }

    public static T LoadSettings<T>(TypeConfig<T> typeConfig) where T : IConfiguration
    {
        var filePath = Path.Combine(BaseDirectory, typeConfig.FileName);

        if (IsRead)
        {
            return typeConfig.Config;
        }

        if (!File.Exists(filePath))
        {
            SaveSettings(typeConfig);
        }

        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StreamReader(filePath);
        var config = (T)(serializer.Deserialize(reader)
                         ?? throw new InvalidOperationException($"Ошибка десериализации: {filePath}"));

        typeConfig.Config = config;

        IsRead = true;

        return config;
    }

    public static void SaveSettings<T>(TypeConfig<T> config) where T : IConfiguration
    {
        try
        {
            var configFileName = Path.Combine(BaseDirectory, config.FileName);
            var directory = Path.GetDirectoryName(configFileName);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var serializer = new XmlSerializer(typeof(EmailConfiguration));

            using var writer = new StreamWriter(configFileName);
            serializer.Serialize(writer, config.Config);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Ошибка сохранения настроек: {ex.Message}");
        }
    }
}