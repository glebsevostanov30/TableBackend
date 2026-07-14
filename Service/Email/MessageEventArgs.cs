using System.Xml;
using UniqueId = MailKit.UniqueId;

namespace TableBackend.Service.Email;

public class MessageEventArgs : EventArgs
{
    public string Subject { get; set; }
    public string From { get; set; }
    public DateTimeOffset? Date { get; set; }
    public UniqueId Uid { get; set; }
}