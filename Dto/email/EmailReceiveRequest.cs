namespace TableBackend.Dto.email;

public class EmailReceiveRequest
{
    public string UserId { get; set; }
    public EmailDto Email { get; set; }
}