namespace TableBackend.Dto.email;

public class AttachmentDto
{
    public string Id { get; set; }
    public string FileName { get; set; }
    public long Size { get; set; }
    public string Url { get; set; }
}