namespace TableBackend.Dto.email;

public class EmailDto
{
    public string Id { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public DateTime ReceivedAt { get; set; }
    public bool IsRead { get; set; }
    public List<AttachmentDto> Attachments { get; set; }
}