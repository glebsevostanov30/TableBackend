namespace TableBackend.Dto.email;

public class CredentialsRequest
{
    public string Host { get; set; } // ex smtp.gmail.com, imap.gmail.com
    public int Port { get; set; } // ex 465, 993
    public string Username { get; set; } // ex viktorhogberg@gmail.com
    public string Password { get; set; } // ex google app password
    public string Folder { get; set; } // ex "INBOX"
}