using MailKit;
using Microsoft.AspNetCore.Mvc;
using TableBackend.Config;
using TableBackend.Dto.email;
using TableBackend.Service.Email;

namespace TableBackend.Controller.Email;

[ApiController]
[Route("Api/[controller]")]
public class EmailController : ControllerBase
{
    // store attachments temporarily in memory for download
    private static readonly Dictionary<string, EmailMessage.Attachment> AttachmentCache = new();

    [HttpGet("get-folders")]
    public IActionResult GetFolders()
    {
        return Ok(MailReceiver.GetFoldersSync());
    }

    // Sending emails, api request ends with /send-email
    [HttpPost("send-email")]
    public async Task<IActionResult> SendEmail(
        [FromForm] string host,
        [FromForm] int port,
        [FromForm] string username,
        [FromForm] string password,
        [FromForm] string to,
        [FromForm] string subject,
        [FromForm] string body,
        [FromForm] List<IFormFile>? attachments)
    {
        try
        {
            var attachmentFiles = new List<FileInfo>();
            if (attachments != null)
            {
                foreach (var formFile in attachments)
                {
                    var tempPath = Path.GetTempFileName();
                    await using (var stream = new FileStream(tempPath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }
                    var fileInfo = new FileInfo(tempPath);
                    attachmentFiles.Add(fileInfo);
                }
            }

            MailSender.SendMail(
                to, subject, body, attachmentFiles
            );

            return Ok(new { status = "success", message = "Email sent successfully" });
        }
        catch (Exception e)
        {
            return StatusCode(500, new { status = "error", message = e.Message });
        }
    }

    [HttpGet("download-attachment/{id}")]
    public IActionResult DownloadAttachment(string id)
    {
        if (!AttachmentCache.TryGetValue(id, out var attachment))
        {
            return NotFound();
        }

        // Return the file with appropriate content type
        return File(attachment.Content, attachment.Type, attachment.Name);
    }
}