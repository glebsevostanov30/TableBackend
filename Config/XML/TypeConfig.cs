using TableBackend.Config.XML.Email;

namespace TableBackend.Config.XML;

public class TypeConfig<T>(string fileName, T config)
{
    public string FileName { get; } = fileName;
    public T Config { get; set; } = config;

    // public static TypeConfig<TelegramConfiguration> Telegram 
    //     => new TypeConfig<TelegramConfiguration>("telegram_settings.xml");
}