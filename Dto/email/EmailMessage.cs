namespace TableBackend.Dto.email;

public class EmailMessage
{
    public string Id {get; set; }
    public string From {get; set; }
    public string Subject {get; set; }
    public string Body {get; set; }
    public string Date {get; set; }

    public List<Attachment>? Attachments { get; set; }

    public class Attachment
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public long Size { get; set; }
        public byte[] Content { get; set; }
    }
}