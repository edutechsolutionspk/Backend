using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using WebBackendProject.Data;
using WebBackendProject.Models;
using WebBackendProject.Other;
using WebBackendProject.Services;


namespace WebBackendProject.Controllers
{

    [Route("api/SheetsController")]
    public class SheetsFileAndFolderController : ControllerBase
    {
        ApplicationDbContext _context;

        FolderAndFilesHandler _handler;
       

        public SheetsFileAndFolderController(ApplicationDbContext context)
        {
            _context = context;
            _handler = new FolderAndFilesHandler(context);
            
        }


        public class SheetsFolderDto
        {
            [JsonPropertyName("folder_name")]
            public string folder_name { get; set; }

            [JsonPropertyName("Category")]
            public string Category { get; set; }

            [JsonPropertyName("module")]
            public string module { get; set; }
        }

        [HttpPost("Sheetsfiles")]
        public async Task<IActionResult> GetFiles([FromForm] string Category, [FromForm] string module, [FromQuery] int page = 1)
        {
            if (string.IsNullOrEmpty(Category) || string.IsNullOrEmpty(module))
            {
                return BadRequest(new { success = false, message = "Category and module are required." });
            }
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            IQueryable<object> query;

            if (module.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                query = _context.sheetsPdfFiles
                    .Where(f => f.Category == Category)
                    .OrderBy(f => f.Id)
                    .Select(f => new { f.FileName, f.Id, ThumbnailUrl = !string.IsNullOrEmpty(f.thumbnailpath) ? $"{baseUrl}{f.thumbnailpath}" : null });
            }
            else if (module.Equals("flash", StringComparison.OrdinalIgnoreCase))
            {
                query = _context.sheetsFlashFiles
                    .Where(f => f.Category == Category)
                    .OrderBy(f => f.Id)
                    .Select(f => new { f.FileName, f.Id });
            }
            else
            {
                return BadRequest(new { success = false, message = "Invalid module type." });
            }

            int pageSize = 15; // You can change this as needed
            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            if (totalPages == 0)
                totalPages = 1;

            // Wrap-around logic
            if (page < 1)
                page = totalPages;
            else if (page > totalPages)
                page = 1;

            var files = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                files,

                currentPage = page,
                totalPages
            });
        }


        [HttpPost("SheetsUploadChunk")]
        public async Task<IActionResult> UploadChunk(
[FromForm] SheetsUploadChunkRequest request)
        {
            string uploadFolderPath = Path.Combine("wwwroot", "uploadedfiles", _handler.CreateModuleFolderByCategory(request.Category, request.module));
            string imagePath = "";
            if (request.file == null || request.file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded." });
            }

            try
            {
                string tempFolder = Path.Combine(uploadFolderPath, "Temp", request.fileName);
                Directory.CreateDirectory(tempFolder);

                string chunkPath = Path.Combine(tempFolder, request.chunkIndex.ToString());
                using (var fileStream = new FileStream(chunkPath, FileMode.Create))
                {
                    await request.file.CopyToAsync(fileStream);
                }

                // If all chunks are uploaded, merge them
                if (Directory.GetFiles(tempFolder).Length == request.totalChunks)
                {
                    string finalFilePath = Path.Combine(uploadFolderPath, request.fileName);

                    using (var finalStream = new FileStream(finalFilePath, FileMode.Create))
                    {
                        for (int i = 0; i < request.totalChunks; i++)
                        {
                            string partPath = Path.Combine(tempFolder, i.ToString());
                            using (var partStream = new FileStream(partPath, FileMode.Open))
                            {
                                await partStream.CopyToAsync(finalStream);
                            }
                        }

                    }


                    string encryptionKey = FileEncryptionHelper.GenerateRandomKey();
                    string iv = FileEncryptionHelper.GenerateRandomIV();

                    // 🔹 Ensure file is closed before encrypting
                    using (var checkStream = new FileStream(finalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // Just open and close to release any locks
                    }

                    // Encrypt the file in place (overwrite)
                    FileEncryptionHelper.EncryptFile(finalFilePath, finalFilePath, encryptionKey, iv);

                    // Ensure encryption is complete before saving metadata
                    if (System.IO.File.Exists(finalFilePath))
                    {
                        object newFile = null;
                        string relativeFilePath = finalFilePath.Substring(8); // Remove "wwwroot" prefix

                        if (IsDuplicateFile(request.fileName, request.module, request.Category))
                        {
                            return BadRequest(new { message = "A file with the same name already exists in this folder." });
                        }
                        if (request.bookImage != null)
                        {
                            string thumbnailPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploadedfiles/SheetsThumbnails");
                            if (!Directory.Exists(thumbnailPath))
                            {
                                Directory.CreateDirectory(thumbnailPath);

                            }

                            string uniqueFileName = $"{Guid.NewGuid()}_{request.bookImage.FileName}";
                            string filePath = Path.Combine(thumbnailPath, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await request.bookImage.CopyToAsync(stream);
                            }

                            imagePath = $"/uploadedfiles/SheetsThumbnails/{uniqueFileName}";
                        }



                        if (request.module.Equals("pdf"))
                        {
                            newFile = new sheetspdffiles
                            {
                                FileName = request.fileName,
                                FilePath = relativeFilePath,
                                Category = request.Category,

                                EncryptionKey = encryptionKey,
                                IV = iv,
                                UploadDate = DateTime.UtcNow,
                                thumbnailpath = imagePath

                            };
                            _context.sheetsPdfFiles.Add((sheetspdffiles)newFile);
                        }
                        else if (request.module.Equals("flash"))
                        {
                            newFile = new sheetsflashfiles
                            {
                                FileName = request.fileName,
                                FilePath = relativeFilePath,
                                Category = request.Category,

                                EncryptionKey = encryptionKey,
                                IV = iv,
                                UploadDate = DateTime.UtcNow,

                            };
                            _context.sheetsFlashFiles.Add((sheetsflashfiles)newFile);

                        }
                        else
                        {
                            return BadRequest(new { message = "Invalid module type. Only 'pdf' and 'flash' are supported." });
                        }

                        await _context.SaveChangesAsync();
                        // Delete temp chunks folder
                        
                        if (Directory.Exists(tempFolder))
                        {
                            Directory.Delete(tempFolder, true);

                        }
                    }


                    return Ok(new
                    {
                        message = "File uploaded and encrypted successfully.",

                    });
                }

                return Ok(new { message = "Chunk uploaded successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        private bool IsDuplicateFile(string fileName, string module, string Category)
        {
            if (module.Equals("pdf"))
            {
                return _context.sheetsPdfFiles.Any(f => f.FileName == fileName && f.Category == Category);
            }
            else if (module.Equals("flash"))
            {
                return _context.sheetsFlashFiles.Any(f => f.FileName == fileName && f.Category == Category);
            }
            return false;
        }

        [HttpDelete("SheetsDeleteFiles")]
        public async Task<IActionResult> DeleteFiles(
    [FromQuery] int id,
    [FromQuery] string module)
        {
            try
            {
                dynamic? file = null;

                if (module.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    file = await _context.sheetsPdfFiles
                        .Where(f => f.Id == id)
                        .Select(f => new { f.Id, f.FilePath })
                        .FirstOrDefaultAsync();
                }
                else if (module.Equals("flash", StringComparison.OrdinalIgnoreCase))
                {
                    file = await _context.sheetsFlashFiles
                        .Where(f => f.Id == id)
                        .Select(f => new { f.Id, f.FilePath })
                        .FirstOrDefaultAsync();
                }

                // ✅ Check if the file exists before proceeding
                if (file == null)
                {
                    return NotFound(new { message = "File not found." });
                }

                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FilePath);

                // ✅ Delete file from the disk
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                // ✅ Remove file from the database
                if (module.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    _context.sheetsPdfFiles.Remove(new sheetspdffiles { Id = file.Id });

                }
                else if (module.Equals("flash", StringComparison.OrdinalIgnoreCase))
                {
                    _context.sheetsFlashFiles.Remove(new sheetsflashfiles { Id = file.Id });
                }

                // ✅ Commit changes to the database
                await _context.SaveChangesAsync();

                return Ok(new { message = "File deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the file.", error = ex.Message });
            }
        }



        [HttpGet("GetCategory")]
        public async Task<IActionResult> GetCategoryByUserId(int userId)
        {
            try
            {
                // Fetch classId based on userId
                var user = await _context.Students
                    .Where(u => u.id == userId)
                    .Select(u => new { u.class_id })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Get category from classId
                string category = GetCategory(user.class_id);

                return Ok(new { success = true, category });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Server error", error = ex.Message });
            }
        }

        // **Helper function to determine Category based on ClassId**
        private string GetCategory(int classId)
        {
            if (classId >= 1 && classId <= 3)
                return "Pre-Elementary";
            else if (classId >= 4 && classId <= 11)
                return "Elementary";
            else if (classId >= 12 && classId <= 13)
                return "Secondary";

            return "Unknown";
        }

        public class SheetsFileDto
        {
            public int Id { get; set; }
            public string FilePath { get; set; }
        }

        public class SheetsUploadChunkRequest
        {

            public string Category { get; set; }

            public string module { get; set; }

            public string foldername { get; set; }


            public IFormFile file { get; set; }

            public string fileName { get; set; }

            public int chunkIndex { get; set; }

            public int totalChunks { get; set; }


            public IFormFile? bookImage { get; set; }


        }

    }
}
