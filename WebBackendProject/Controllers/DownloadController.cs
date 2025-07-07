using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using WebBackendProject.Data;
using WebBackendProject.Other;

namespace WebBackendProject.Controllers
{
    [Route("api/DownloadController")]
    public class DownloadController : ControllerBase
    {
        ApplicationDbContext _context;
        private readonly FileEncryptionHelper fileEncryptionHelper = new FileEncryptionHelper();

        public  DownloadController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("DownloadPdf")]
        public async Task<IActionResult> DownloadPdf([FromBody] FileDownloadRequest request)
        {
            var fileRecord = await _context.pdffiles
                .Where(f => f.Id == request.FileId)
                .Select(f => new { f.FilePath, f.FileName,f.EncryptionKey, f.IV })
                .FirstOrDefaultAsync();

            if (fileRecord == null)
                return NotFound("File not found.");

            string encryptedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileRecord.FilePath);
            if (!System.IO.File.Exists(encryptedFilePath))
                return NotFound("File does not exist.");

            byte[] key = Convert.FromBase64String(fileRecord.EncryptionKey);
            byte[] iv = Convert.FromBase64String(fileRecord.IV);

            // Get a decrypted stream (DO NOT buffer into memory)
            var decryptedStream = fileEncryptionHelper.DecryptFileStream(encryptedFilePath, key, iv);


            

            // Return stream with download headers
            return new FileStreamResult(decryptedStream, "application/pdf")
            {
                FileDownloadName = fileRecord.FileName
            };
        }

        // DTO for secure body request
        public class FileDownloadRequest
        {
            public int FileId { get; set; }
        }
    }
}
