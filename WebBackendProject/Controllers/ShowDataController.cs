using Microsoft.AspNetCore.Mvc;
using WebBackendProject.Data;
using Microsoft.EntityFrameworkCore;
using WebBackendProject.Other;
using System.Security.Cryptography;


namespace WebBackendProject.Controllers
{
    [Route("api/ShowDataController")]
    public class ShowDataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FileEncryptionHelper fileEncryptionHelper = new FileEncryptionHelper();
        public ShowDataController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("view/{fileId}")]
        public async Task<IActionResult> ViewPdf(int fileId)
        {
            var fileRecord = await _context.pdffiles
                .Where(f => f.Id == fileId)
                .Select(f => new { f.FilePath, f.EncryptionKey, f.IV })
                .FirstOrDefaultAsync();

            if (fileRecord == null)
                return NotFound("File not found.");

            string encryptedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileRecord.FilePath);
            if (!System.IO.File.Exists(encryptedFilePath))
                return NotFound("File does not exist.");

            byte[] key = Convert.FromBase64String(fileRecord.EncryptionKey);
            byte[] iv = Convert.FromBase64String(fileRecord.IV);

            // Open the encrypted file and create a stream for decryption
            var decryptedStream = fileEncryptionHelper.DecryptFileStream(encryptedFilePath, key, iv);

            // Set the response headers to prevent downloading
            Response.Headers["Content-Disposition"] = "inline"; // Display in-browser

            // Set cache control to prevent caching
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate"; // Prevent caching
            Response.Headers["Pragma"] = "no-cache"; // For HTTP/1.0 compatibility
            Response.Headers["Expires"] = "0"; // Ensures the content is always fresh

            // To improve performance, stream the file in chunks, especially for large files
            return new FileStreamResult(decryptedStream, "application/pdf");
        }

        [HttpGet("VideoFile/{fileId}")]
        public async Task<IActionResult> ViewVideo(int fileId)
        {
            var fileRecord = await _context.VideoFiles
                .Where(f => f.Id == fileId)
                .Select(f => new { f.FilePath, f.EncryptionKey, f.IV })
                .FirstOrDefaultAsync();

            if (fileRecord == null)
                return NotFound("Video not found.");

            string encryptedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileRecord.FilePath);
            
            byte[] key = Convert.FromBase64String(fileRecord.EncryptionKey);
            byte[] iv = Convert.FromBase64String(fileRecord.IV);

            var request = HttpContext.Request;
            long totalLength = new FileInfo(encryptedFilePath).Length;

            if (!request.Headers.ContainsKey("Range"))
            {
                // No Range header: fallback to full stream
                var fullStream = fileEncryptionHelper.DecryptFileStream(encryptedFilePath, key, iv);
                return File(fullStream, "video/mp4");
            }

            // Extract range
            string rangeHeader = request.Headers["Range"];
            var range = rangeHeader.Replace("bytes=", "").Split('-');
            long start = long.Parse(range[0]);
            long end = (range.Length > 1 && !string.IsNullOrWhiteSpace(range[1]))
                ? long.Parse(range[1])
                : totalLength - 1;

            long chunkSize = end - start + 1;

            // AES block size = 16 bytes
            const int blockSize = 16;
            long alignedStart = start - (start % blockSize);
            long offset = start - alignedStart;
            long alignedLength = chunkSize + offset;

            using var inputStream = new FileStream(encryptedFilePath, FileMode.Open, FileAccess.Read);
            inputStream.Seek(alignedStart, SeekOrigin.Begin);

            byte[] encryptedBuffer = new byte[alignedLength];
            await inputStream.ReadAsync(encryptedBuffer, 0, encryptedBuffer.Length);

            byte[] decryptedBuffer;

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None; // Because we're manually aligning

                using var decryptor = aes.CreateDecryptor();
                decryptedBuffer = decryptor.TransformFinalBlock(encryptedBuffer, 0, encryptedBuffer.Length);
            }

            byte[] requestedChunk = decryptedBuffer.Skip((int)offset).Take((int)chunkSize).ToArray();

            Response.StatusCode = 206;
            Response.Headers["Content-Range"] = $"bytes {start}-{end}/{totalLength}";
            Response.Headers["Accept-Ranges"] = "bytes";
            Response.ContentLength = requestedChunk.Length;

            return File(new MemoryStream(requestedChunk), "video/mp4", enableRangeProcessing: false);
        }

        [HttpGet("video/{fileId}")]
        public async Task<IActionResult> ViewEdutechVideo(int fileId)
        {
            var fileRecord = await _context.Teachervideos
                .Where(f => f.Id == fileId)
                .Select(f => new { f.FilePath, f.EncryptionKey, f.IV })
                .FirstOrDefaultAsync();

            if (fileRecord == null)
                return NotFound("Video not found.");

            string encryptedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileRecord.FilePath);

            byte[] key = Convert.FromBase64String(fileRecord.EncryptionKey);
            byte[] iv = Convert.FromBase64String(fileRecord.IV);

            var request = HttpContext.Request;
            long totalLength = new FileInfo(encryptedFilePath).Length;

            if (!request.Headers.ContainsKey("Range"))
            {
                // No Range header: fallback to full stream
                var fullStream = fileEncryptionHelper.DecryptFileStream(encryptedFilePath, key, iv);
                return File(fullStream, "video/mp4");
            }

            // Extract range
            string rangeHeader = request.Headers["Range"];
            var range = rangeHeader.Replace("bytes=", "").Split('-');
            long start = long.Parse(range[0]);
            long end = (range.Length > 1 && !string.IsNullOrWhiteSpace(range[1]))
                ? long.Parse(range[1])
                : totalLength - 1;

            long chunkSize = end - start + 1;

            // AES block size = 16 bytes
            const int blockSize = 16;
            long alignedStart = start - (start % blockSize);
            long offset = start - alignedStart;
            long alignedLength = chunkSize + offset;

            using var inputStream = new FileStream(encryptedFilePath, FileMode.Open, FileAccess.Read);
            inputStream.Seek(alignedStart, SeekOrigin.Begin);

            byte[] encryptedBuffer = new byte[alignedLength];
            await inputStream.ReadAsync(encryptedBuffer, 0, encryptedBuffer.Length);

            byte[] decryptedBuffer;

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None; // Because we're manually aligning

                using var decryptor = aes.CreateDecryptor();
                decryptedBuffer = decryptor.TransformFinalBlock(encryptedBuffer, 0, encryptedBuffer.Length);
            }

            byte[] requestedChunk = decryptedBuffer.Skip((int)offset).Take((int)chunkSize).ToArray();

            Response.StatusCode = 206;
            Response.Headers["Content-Range"] = $"bytes {start}-{end}/{totalLength}";
            Response.Headers["Accept-Ranges"] = "bytes";
            Response.ContentLength = requestedChunk.Length;

            return File(new MemoryStream(requestedChunk), "video/mp4", enableRangeProcessing: false);
        }

        [HttpGet("ViewFiles/{fileId}")]
        public async Task<IActionResult> ViewFiles(int fileId)
        {
            var fileRecord = await _context.EcourseBook
               .Where(f => f.Id == fileId)
               .Select(f =>  f.WebsiteLink)
               .FirstOrDefaultAsync();

            if (fileRecord == null )
            {
                return NotFound(new { message = "File not found or no website link available." });
            }

            return Ok(new {fileRecord });

        }


        [HttpGet("ViewFlash/{fileId}")]
        public async Task<IActionResult> ViewFlash(int fileId)
        {


            // Fetch file record, including AES key & IV
            var fileRecord = await _context.flashFiles // Ensure the correct table name
                .Where(f => f.Id == fileId)  // Fixed query issue
                .Select(f => new { f.FilePath, f.EncryptionKey, f.IV })
                .FirstOrDefaultAsync();

            if (fileRecord == null)
            {
                Console.WriteLine("❌ File record not found.");
                return NotFound("File not found.");
            }

            string encryptedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileRecord.FilePath);

            if (!System.IO.File.Exists(encryptedFilePath))
            {

                return NotFound("File does not exist.");
            }


            // Convert Base64 key and IV to byte arrays
            byte[] key = Convert.FromBase64String(fileRecord.EncryptionKey);
            byte[] iv = Convert.FromBase64String(fileRecord.IV);

            // Decrypt the file
            byte[] decryptedBytes = fileEncryptionHelper.DecryptFile(encryptedFilePath, key, iv);

            // Serve the decrypted SWF file inline
            Response.Headers["Content-Disposition"] = "inline";
            return File(decryptedBytes, "application/x-shockwave-flash");

        }

        [HttpGet("SheetsViewFlash/{fileId}")]
        public async Task<IActionResult> SheetsViewFlash(int fileId)
        {


            // Fetch file record, including AES key & IV
            var fileRecord = await _context.sheetsFlashFiles // Ensure the correct table name
                .Where(f => f.Id == fileId)  // Fixed query issue
                .Select(f => new { f.FilePath, f.EncryptionKey, f.IV })
                .FirstOrDefaultAsync();

            if (fileRecord == null)
            {
                Console.WriteLine("❌ File record not found.");
                return NotFound("File not found.");
            }

            string encryptedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileRecord.FilePath);

            if (!System.IO.File.Exists(encryptedFilePath))
            {

                return NotFound("File does not exist.");
            }


            // Convert Base64 key and IV to byte arrays
            byte[] key = Convert.FromBase64String(fileRecord.EncryptionKey);
            byte[] iv = Convert.FromBase64String(fileRecord.IV);

            // Decrypt the file
            byte[] decryptedBytes = fileEncryptionHelper.DecryptFile(encryptedFilePath, key, iv);

            // Serve the decrypted SWF file inline
            Response.Headers["Content-Disposition"] = "inline";
            return File(decryptedBytes, "application/x-shockwave-flash");

        }


        [HttpGet("SheetsViewPdf/{fileId}")]
        public async Task<IActionResult> SheetsViewPdf(int fileId)
        {
            // Fetch file record, including AES key & IV
            var fileRecord = await _context.sheetsPdfFiles
                 .Where(f => f.Id == fileId)
                 .Select(f => new { f.FilePath, f.EncryptionKey, f.IV })
                 .FirstOrDefaultAsync();

            if (fileRecord == null)
                return NotFound("File not found.");

            string encryptedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileRecord.FilePath);
            if (!System.IO.File.Exists(encryptedFilePath))
                return NotFound("File does not exist.");

            byte[] key = Convert.FromBase64String(fileRecord.EncryptionKey);
            byte[] iv = Convert.FromBase64String(fileRecord.IV);

            // Open the encrypted file and create a stream for decryption
            var decryptedStream = fileEncryptionHelper.DecryptFileStream(encryptedFilePath, key, iv);

            // Set the response headers to prevent downloading
            Response.Headers["Content-Disposition"] = "inline"; // Display in-browser

            // Set cache control to prevent caching
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate"; // Prevent caching
            Response.Headers["Pragma"] = "no-cache"; // For HTTP/1.0 compatibility
            Response.Headers["Expires"] = "0"; // Ensures the content is always fresh

            // To improve performance, stream the file in chunks, especially for large files
            return new FileStreamResult(decryptedStream, "application/pdf");
        }
        [HttpGet("TeacherViewPdf/{fileId}")]
        public async Task<IActionResult> TeacherViewPdf(int fileId)
        {
            // Fetch file record, including AES key & IV
            var fileRecord = await _context.Teacherpdf
                 .Where(f => f.Id == fileId)
                 .Select(f => new { f.FilePath, f.EncryptionKey, f.IV })
                 .FirstOrDefaultAsync();

            if (fileRecord == null)
                return NotFound("File not found.");

            string encryptedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileRecord.FilePath);
            if (!System.IO.File.Exists(encryptedFilePath))
                return NotFound("File does not exist.");

            byte[] key = Convert.FromBase64String(fileRecord.EncryptionKey);
            byte[] iv = Convert.FromBase64String(fileRecord.IV);

            // Open the encrypted file and create a stream for decryption
            var decryptedStream = fileEncryptionHelper.DecryptFileStream(encryptedFilePath, key, iv);

            // Set the response headers to prevent downloading
            Response.Headers["Content-Disposition"] = "inline"; // Display in-browser

            // Set cache control to prevent caching
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate"; // Prevent caching
            Response.Headers["Pragma"] = "no-cache"; // For HTTP/1.0 compatibility
            Response.Headers["Expires"] = "0"; // Ensures the content is always fresh

            // To improve performance, stream the file in chunks, especially for large files
            return new FileStreamResult(decryptedStream, "application/pdf");
        }

    }
}


