using Microsoft.AspNetCore.Mvc;

namespace TableBackend.Controller;

[ApiController]
[Route("Api/[controller]")]
public class UploadController(IWebHostEnvironment env) : ControllerBase
{
    private readonly string _appRoot = AppDomain.CurrentDomain.BaseDirectory;
    
    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadFiles([FromForm] IFormFileCollection? files)
    {
        if (files == null || files.Count == 0)
            return BadRequest("Файлы не переданы");

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

        return Ok(new { Message = "Файлы загружены", Files = uploadedFiles });
    }
    
    // Новый метод для локального пути
    [HttpPost("bypath")]
    public IActionResult UploadByPath([FromBody] PathRequest request)
    {
        if (string.IsNullOrEmpty(request.Path))
            return BadRequest("Path is required.");

        // Проверяем, существует ли файл по указанному абсолютному пути
        if (!System.IO.File.Exists(request.Path))
            return NotFound($"File not found at path: {request.Path}");

        // Здесь можно прочитать файл и обработать его
        // Например, прочитать Excel, или скопировать в нужную папку
        // Важно: убедиться, что путь безопасен (не выходит за пределы разрешённых директорий)
        // Рекомендуется проверять, что путь находится в разрешённой корневой папке.

        // Простой пример: копируем в папку uploads
        var uploadsFolder = Path.Combine(_appRoot, "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = Path.GetFileName(request.Path);
        var destPath = Path.Combine(uploadsFolder, Guid.NewGuid() + "_" + fileName);
        System.IO.File.Copy(request.Path, destPath);

        return Ok(new { message = $"File copied from local path: {request.Path}" });
    }
    
    public class PathRequest
    {
        public string Path { get; set; }
    }

}