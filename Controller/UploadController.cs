using Microsoft.AspNetCore.Mvc;

namespace TableBackend.Controller;

[ApiController]
[Route("Api/Table/[action]")]
public class UploadController(IWebHostEnvironment env) : ControllerBase
{
    private readonly string _appRoot = AppDomain.CurrentDomain.BaseDirectory;

    [HttpPost]
    // [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload([FromForm] IFormFileCollection? files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { Message = "Файлы не переданы", Success = false });

        var uploadPath = Path.Combine(_appRoot, "uploads");
        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        var uploadedFiles = new List<string>();

        foreach (var file in files)
        {
            if (file.Length <= 0) continue;

            // Генерируем уникальное имя, чтобы избежать конфликтов
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            uploadedFiles.Add(fileName);
        }

        return Ok(new { Message = "Файлы загружены", Success = true, Files = uploadedFiles });
    }
}