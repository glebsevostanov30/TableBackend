using TableBackend.Config.XML.Email;

namespace TableBackend.Config.XML;

public static class XmlConfigs
{
    public static TypeConfig<EmailConfiguration> Email => new("email_settings.xml",
        new EmailConfiguration
        {
            Settings = new EmailConfiguration.EmailSettings
            {
                ImapServer = "imap.yandex.ru",
                ImapPort = 993,
                ImapUseSsl = true,
                SmtpServer =  "smtp.yandex.ru",
                SmtpPort = 465,
                SmtpUseSsl = true,
                EmailAddress = "Programm-Python@yandex.ru",
                Password = "kqvtoquajahfflje",
                LocalDirectoryDownload = "Download",
                EmailDirectoryDownload = "Работа",
            }
        }
    );
}