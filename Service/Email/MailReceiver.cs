using System.Net;
using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using TableBackend.Config.XML;
using TableBackend.Dto.email;

namespace TableBackend.Service.Email;

/// <summary>
/// MailReceiver service that retrieves the latest emails from a specified IMAP folder
/// </summary>
public partial class MailReceiver
{
    private const int MaxMessagesToRetrieve = 10;
    private const int ConnectionTimeoutSeconds = 5;
    private const int FetchSizeBytes = 1048576; // 1MB

    [GeneratedRegex("<.*?>")]
    private static partial Regex PlainText();

    [GeneratedRegex("\\s+")]
    private static partial Regex PlainTextWithoutSpace();

    /// <summary>
    /// Retrieves the most recent emails from the specified IMAP folder
    /// </summary>
    /// <param name="host">IMAP server host</param>
    /// <param name="port">IMAP server port</param>
    /// <param name="username">Email account username</param>
    /// <param name="password">Email account password</param>
    /// <param name="folderName">Folder to retrieve emails from (defaults to INBOX)</param>
    /// <returns>List of email messages</returns>
    /// <exception cref="ImapProtocolException">Thrown when IMAP protocol error occurs</exception>
    /// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
    public static async Task<List<EmailMessage>> ReceiveMailAsListAsync()
    {
        var emailMessages = new List<EmailMessage>();


        using var client = new ImapClient();

        try
        {
            // Configure connection settings
            // client.Timeout = TimeSpan.FromSeconds(ConnectionTimeoutSeconds).Milliseconds;
            var config = XmlSettingService.LoadSettings(XmlConfigs.Email).Settings;

            // Connect to the IMAP server
            await client.ConnectAsync(config.ImapServer, config.ImapPort, config.ImapUseSsl);

            // Authenticate
            await client.AuthenticateAsync(config.EmailAddress, config.Password);


            // Open the specified folder or default to INBOX
            var folder = string.IsNullOrWhiteSpace(config.EmailDirectoryDownload)
                ? client.Inbox
                : await client.GetFolderAsync(config.EmailDirectoryDownload);

            await folder.OpenAsync(FolderAccess.ReadOnly);

            // Get the most recent messages (limited to MaxMessagesToRetrieve)
            var messages = await GetRecentMessagesAsync(folder);

            // Process each message
            foreach (var message in messages)
            {
                var emailMessage = await ProcessMessageAsync(message);
                emailMessages.Add(emailMessage);
            }
        }
        finally
        {
            // Disconnect properly
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true);
            }
        }

        return emailMessages;
    }

    /// <summary>
    /// Gets the most recent messages from a folder
    /// </summary>
    private static async Task<IList<MimeMessage>> GetRecentMessagesAsync(IMailFolder folder)
    {
        // Get total message count
        var totalCount = folder.Count;

        if (totalCount == 0)
        {
            return new List<MimeMessage>();
        }

        // Determine how many messages to fetch
        var fetchCount = Math.Min(MaxMessagesToRetrieve, totalCount);

        // Get the most recent messages (by UID order)
        var uids = await folder.SearchAsync(SearchQuery.All);
        var recentUids = uids.Skip(Math.Max(0, uids.Count - fetchCount)).ToList();

        // Fetch the messages with their content
        var messages = await folder.FetchAsync(recentUids, MessageSummaryItems.Full | MessageSummaryItems.UniqueId);

        var result = new List<MimeMessage>();
        foreach (var summary in messages.OrderByDescending(m => m.Date))
        {
            try
            {
                var message = await folder.GetMessageAsync(summary.UniqueId);
                result.Add(message);
            }
            catch (Exception ex)
            {
                // Log error but continue processing other messages
                // Logger.LogError($"Failed to retrieve message {summary.UniqueId}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Processes an individual MimeMessage and converts it to EmailMessage DTO
    /// </summary>
    private static async Task<EmailMessage> ProcessMessageAsync(MimeMessage message)
    {
        var emailMessage = new EmailMessage
        {
            Id = message.MessageId ?? Guid.NewGuid().ToString(),
            Subject = string.IsNullOrWhiteSpace(message.Subject) ? "No subject" : message.Subject,
            From = GetSenderAddress(message.From),
            Date = FormatDate(message.Date),
            Body = await ExtractBodyTextAsync(message),
            Attachments = await ExtractAttachmentsAsync(message)
        };

        return emailMessage;
    }

    /// <summary>
    /// Extracts the sender's email address from the message
    /// </summary>
    private static string GetSenderAddress(InternetAddressList? fromAddresses)
    {
        if (fromAddresses == null || fromAddresses.Count == 0)
        {
            return "Unknown sender";
        }

        var mailboxAddress = fromAddresses.OfType<MailboxAddress>().FirstOrDefault();
        return mailboxAddress?.ToString() ?? fromAddresses[0].ToString();
    }

    /// <summary>
    /// Formats the message sent date
    /// </summary>
    private static string FormatDate(DateTimeOffset? date)
    {
        return date?.LocalDateTime.ToString("MMM dd, yyyy HH:mm") ?? "Unknown date";
    }

    /// <summary>
    /// Extracts the plain text body from the message
    /// </summary>
    private static async Task<string> ExtractBodyTextAsync(MimeMessage message)
    {
        try
        {
            var htmlBody = message.HtmlBody;
            if (!string.IsNullOrWhiteSpace(htmlBody))
            {
                return StripHtml(htmlBody);
            }

            var textBody = message.TextBody;
            if (!string.IsNullOrWhiteSpace(textBody))
            {
                return textBody;
            }

            if (message.Body is not Multipart multipart) return "No body content available";

            foreach (var part in multipart)
            {
                if (part is not TextPart textPart) continue;
                if (textPart.IsPlain)
                {
                    var text = textPart.Text;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
                else if (textPart.IsHtml)
                {
                    var html = textPart.Text;
                    if (!string.IsNullOrWhiteSpace(html))
                    {
                        return StripHtml(html);
                    }
                }
            }

            return "No body content available";
        }
        catch (Exception ex)
        {
            // Log error
            // Logger.LogError($"Failed to extract body: {ex.Message}");
            return "Error! Not able to display message content";
        }
    }

    /// <summary>
    /// Extracts attachments from the message
    /// </summary>
    private static async Task<List<EmailMessage.Attachment>?> ExtractAttachmentsAsync(MimeMessage message)
    {
        var attachments = new List<EmailMessage.Attachment>();
        try
        {
            if (!message.Attachments.Any())
            {
                return null;
            }

            foreach (var attachment in message.Attachments)
            {
                try
                {
                    if (attachment is not MimePart mimePart) continue;

                    var fileName = mimePart.FileName ?? "attachment.dat";

                    using var memoryStream = new MemoryStream();

                    if (mimePart.Content == null) continue;

                    await mimePart.Content.DecodeToAsync(memoryStream);
                    var content = memoryStream.ToArray();

                    var attachmentDto = new EmailMessage.Attachment
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = fileName,
                        Type = mimePart.ContentType.MimeType ?? "application/octet-stream",
                        Size = content.Length,
                        Content = content
                    };

                    attachments.Add(attachmentDto);
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other attachments
                    // Logger.LogError($"Failed to extract attachment: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            // Log error
            // Logger.LogError($"Failed to process attachments: {ex.Message}");
            return null;
        }

        return attachments.Count != 0 ? attachments : null;
    }

    /// <summary>
    /// Simple HTML stripper to get plain text from HTML body
    /// </summary>
    private static string StripHtml(string html)
    {
        // Remove HTML tags
        var plainText = PlainText().Replace(html, " ");
        // Replace multiple spaces with single space
        plainText = PlainTextWithoutSpace().Replace(plainText, " ");
        // Decode HTML entities
        plainText = WebUtility.HtmlDecode(plainText);
        return plainText.Trim();
    }

    /// <summary>
    /// Synchronous wrapper for backward compatibility
    /// </summary>
    public static List<EmailMessage> ReceiveMailAsList()
    {
        return Task.Run(async () => await ReceiveMailAsListAsync()).Result;
    }
    
    public static async Task<IList<IMailFolder>> GetFolders()
    {
        using var client = new ImapClient();
        var config = XmlSettingService.LoadSettings(XmlConfigs.Email).Settings;
        
        var personalNamespace = client.PersonalNamespaces[0];
        
        // Connect to the IMAP server
        await client.ConnectAsync(config.ImapServer, config.ImapPort, config.ImapUseSsl);

        // Authenticate
        await client.AuthenticateAsync(config.EmailAddress, config.Password);
        
        return await client.GetFoldersAsync(personalNamespace);
    }
    
    public static IList<IMailFolder> GetFoldersSync()
    {
        return Task.Run(async () => await GetFolders()).Result;
    }
}