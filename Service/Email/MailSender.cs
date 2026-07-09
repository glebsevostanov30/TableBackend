using System.Net;
using System.Net.Mail;
using TableBackend.Config.XML;

namespace TableBackend.Service.Email;

public static class MailSender
{
    public static void SendMail(string to, string subject, string bodyText, List<FileInfo>? attachments)
    {
        try
        {
            var config = XmlSettingService.LoadSettings(XmlConfigs.Email).Settings;
            
            // Создаем настройки SMTP клиента
            var client = new SmtpClient(config.SmtpServer, config.SmtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(config.EmailAddress, config.Password),
                Timeout = 10000 // 10 секунд таймаут
            };

            // Создаем письмо
            var message = new MailMessage
            {
                From = new MailAddress(config.EmailAddress),
                Subject = subject,
                Body = bodyText,
                IsBodyHtml = false // Если требуется HTML - установить true
            };

            // Добавляем получателя
            message.To.Add(to);

            // Проверяем наличие вложений
            if (attachments is { Count: > 0 })
            {
                foreach (var file in attachments)
                {
                    if (!file.Exists) continue;
                    var attachment = new Attachment(file.FullName);
                    message.Attachments.Add(attachment);
                }
            }

            // Отправляем письмо
            client.Send(message);

            // Очищаем ресурсы
            message.Dispose();
            client.Dispose();
        }
        catch (SmtpException ex)
        {
            Console.WriteLine($"SMTP ошибка: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка отправки письма: {ex.Message}");
            throw;
        }
    }
}