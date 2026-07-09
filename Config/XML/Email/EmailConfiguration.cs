using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace TableBackend.Config.XML.Email;

[Serializable]
[XmlRoot("EmailConfiguration")]
public class EmailConfiguration: IConfiguration
{
    [XmlElement("Settings")]
    public required EmailSettings Settings { get; set; }

    public class EmailSettings
    {
        [Required(ErrorMessage = "Адрес сервера IMAP обязателен.")]
        public required string ImapServer { get; set; } = "imap.yandex.ru";

        [Required(ErrorMessage = "Порт IMAP обязателен.")]
        [Range(1, 65535, ErrorMessage = "Порт должен быть числом от 1 до 65535.")]
        public required int ImapPort { get; set; } = 993;

        public required bool ImapUseSsl { get; set; } = true;

        [Required(ErrorMessage = "Адрес сервера SMTP обязателен.")]
        public required string SmtpServer { get; set; } =  "smtp.yandex.ru";

        [Required(ErrorMessage = "Порт SMTP обязателен.")]
        [Range(1, 65535, ErrorMessage = "Порт должен быть числом от 1 до 65535.")]
        public required int SmtpPort { get; set; } = 465;

        public required bool SmtpUseSsl { get; set; } = true;

        [Required(ErrorMessage = "Email обязателен.")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email.")]
        public required string EmailAddress { get; set; } = "Programm-Python@yandex.ru";

        [Required(ErrorMessage = "Пароль обязателен.")]
        public required string Password { get; set; } = "kqvtoquajahfflje";

        [Required(ErrorMessage = "Папка для сохранения файлов не задана")]
        public required string LocalDirectoryDownload { get; set; } = "Download";

        [Required(ErrorMessage = "Папка почты не задана")]
        public required string EmailDirectoryDownload { get; set; } = "Работа";
    }
}