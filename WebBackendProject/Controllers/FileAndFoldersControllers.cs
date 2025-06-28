using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using WebBackendProject.Data;
using WebBackendProject.Models;
using WebBackendProject.Models.Dto;
using WebBackendProject.Other;



namespace WebBackendProject.Controllers
{

    [Route("api/FileAndFoldersControllers")]
    public class FileAndFoldersControllers : ControllerBase
    {
        ApplicationDbContext _context;
        FolderAndFilesHandler _handler;
        public FileAndFoldersControllers(ApplicationDbContext context)
        {
            _context = context;
            _handler = new FolderAndFilesHandler(context);
        }
        [HttpPost]
        public IActionResult SetFolderData([FromBody] FolderDto folderDto)
        {
            if (string.IsNullOrEmpty(folderDto.FolderName) || folderDto.SubjectId == 0 || folderDto.ClassId == 0)
            {
                return BadRequest(new { success = false, message = "Invalid input data." });
            }

            // Check for duplicate folder
            if (IsDuplicateFolder(folderDto.FolderName, folderDto.SubjectId, folderDto.ClassId, folderDto.Module))
            {
                return BadRequest(new { success = false, message = "A folder with the same name already exists." });
            }

            // Create folder and save to database
            var folder = new folders
            {
                folder_name = folderDto.FolderName,
                folder_path = _handler.CreateFolder(folderDto.ClassId, folderDto.SubjectId, folderDto.Module, folderDto.FolderName, null, folderDto.schoolId),
                subject_id = folderDto.SubjectId,
                class_id = folderDto.ClassId,
                module = folderDto.Module,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
                school_id = folderDto.schoolId,
                created_by = folderDto.adminId
            };

            _context.Folders.Add(folder);
            _context.SaveChanges();

            return Ok(new { success = true, message = "Folder created successfully!" });
        }

        // Function to check if a folder already exists
        private bool IsDuplicateFolder(string folderName, int subjectId, int classId, string module)
        {
            return _context.Folders.Any(f =>
                f.folder_name == folderName &&
                f.subject_id == subjectId &&
                f.class_id == classId &&
                f.school_id == classId &&
                f.module == module
            );
        }
        public class FolderDto
        {
            [JsonPropertyName("folder_name")]
            public string FolderName { get; set; }

            [JsonPropertyName("subject_id")]
            public int SubjectId { get; set; }

            [JsonPropertyName("class_id")]
            public int ClassId { get; set; }

            [JsonPropertyName("module")]
            public string Module { get; set; }
            public int schoolId { get; set; }
            public int adminId { get; set; }
        }

        [HttpPost("folders")]
        public async Task<IActionResult> GetFolderNames([FromBody] FolderRequestDto folder)
        {

            var folders = await _context.Folders
                .Where(f => f.school_id == folder.SchoolId && f.class_id == folder.ClassId && f.subject_id == folder.SubjectId && f.module == folder.Module)
                .Select(f => new { id = f.id, name = f.folder_name })
                .Distinct()
                .ToListAsync();

            if (!folders.Any())
            {
                return NotFound(new { success = false, message = "No folders found." });
            }

            return Ok(new { success = true, folders });
        }

        [HttpPost("files")]
        public async Task<IActionResult> GetFiles([FromBody] FileRequestDto file)
        {
            var files = new List<object>();

            if (file.Module.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                files = await _context.pdffiles
                    .Where(f => f.school_id == file.SchoolId && f.ClassId == file.ClassId && f.SubjectId == file.SubjectId && f.FolderName == file.FolderName)
                    .Select(f => new
                    {
                        f.FileName,
                        f.FolderName,
                        f.Id
                    })
                    .ToListAsync<object>();
            }
            else if (file.Module.Equals("flash", StringComparison.OrdinalIgnoreCase))
            {
                files = await _context.flashFiles
                    .Where(f => f.school_id == file.SchoolId && f.ClassId == file.ClassId && f.SubjectId == file.SubjectId && f.FolderName == file.FolderName)
                    .Select(f => new
                    {
                        f.FileName,
                        f.FolderName,
                        f.Id
                    })
                    .ToListAsync<object>();
            }
            else if (file.Module.Equals("video", StringComparison.OrdinalIgnoreCase))
            {
                files = await _context.VideoFiles
                    .Where(f => f.school_id == file.SchoolId && f.ClassId == file.ClassId && f.SubjectId == file.SubjectId && f.FolderName == file.FolderName)
                    .Select(f => new
                    {
                        f.FileName,
                        f.FolderName,
                        f.Id
                    })
                    .ToListAsync<object>();
            }

            if (!files.Any())
            {
                return NotFound(new { success = false, message = "No files found in this folder." });
            }

            return Ok(new { success = true, files });
        }

        [HttpPost("UploadChunk")]
        public async Task<IActionResult> UploadChunk([FromForm] UploadChunkRequest request)
        {
            string uploadFolderPath = Path.Combine("wwwroot", "uploadedfiles", _handler.CreateFolder(request.classid, request.subjectid, request.module, request.foldername, null, request.schoolid));

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
                    // Delete temp chunks folder
                    if (Directory.Exists(tempFolder))
                    {
                        Directory.Delete(tempFolder, true);
                    }
                    string encryptionKey = FileEncryptionHelper.GenerateRandomKey();
                    string iv = FileEncryptionHelper.GenerateRandomIV();
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

                        if (IsDuplicateFile(request.fileName, request.foldername, request.module, request.classid, request.subjectid, request.schoolid))
                        {
                            return BadRequest(new { message = "A file with the same name already exists in this folder." });
                        }

                        if (request.module.Equals("pdf"))
                        {
                            newFile = new pdfFiles
                            {
                                FileName = request.fileName,
                                FilePath = relativeFilePath,
                                ClassId = request.classid,
                                SubjectId = request.subjectid,
                                FolderName = request.foldername,
                                EncryptionKey = encryptionKey,
                                IV = iv,
                                UploadDate = DateTime.UtcNow,
                                school_id = request.schoolid,
                                uploaded_by_master = request.adminId
                            };
                            _context.pdffiles.Add((pdfFiles)newFile);
                        }
                        else if (request.module.Equals("flash"))
                        {
                            newFile = new FlashFiles
                            {
                                FileName = request.fileName,
                                FilePath = relativeFilePath,
                                ClassId = request.classid,
                                SubjectId = request.subjectid,
                                FolderName = request.foldername,
                                EncryptionKey = encryptionKey,
                                IV = iv,
                                UploadDate = DateTime.UtcNow,
                                IsMainFile = 1,
                                school_id = request.schoolid,
                                uploaded_by_master = request.adminId
                            };
                            _context.flashFiles.Add((FlashFiles)newFile);
                        }
                        else if (request.module.Equals("video"))
                        {
                            newFile = new videofiles
                            {
                                FileName = request.fileName,
                                FilePath = relativeFilePath,
                                ClassId = request.classid,
                                SubjectId = request.subjectid,
                                FolderName = request.foldername,
                                EncryptionKey = encryptionKey,
                                IV = iv,
                                UploadDate = DateTime.UtcNow,
                                school_id = request.schoolid,
                                uploaded_by_master = request.adminId
                            };
                            _context.VideoFiles.Add((videofiles)newFile);
                        }

                        else
                        {
                            return BadRequest(new { message = "Invalid module type. Only 'pdf' and 'flash' are supported." });
                        }

                        await _context.SaveChangesAsync();
                    }

                    return Ok(new
                    {
                        message = "File uploaded and encrypted successfully.",
                        filePath = finalFilePath
                    });
                }

                return Ok(new { message = "Chunk uploaded successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        private bool IsDuplicateFile(string fileName, string folderName, string module, int classid, int subjectid, int schoolid)
        {
            if (module.Equals("pdf"))
            {
                return _context.pdffiles.Any(f => f.FileName == fileName && f.FolderName == folderName && f.ClassId == classid && f.SubjectId == subjectid && f.school_id == schoolid);
            }
            else if (module.Equals("flash"))
            {
                return _context.flashFiles.Any(f => f.FileName == fileName && f.FolderName == folderName && f.ClassId == classid && f.SubjectId == subjectid && f.school_id == schoolid);
            }
            else if (module.Equals("video"))
            {
                return _context.VideoFiles.Any(f => f.FileName == fileName && f.FolderName == folderName && f.ClassId == classid && f.SubjectId == subjectid && f.school_id == schoolid);
            }
            return false;
        }

        [HttpDelete("DeleteFiles")]
        public async Task<IActionResult> DeleteFiles(
    [FromQuery] int id,
    [FromQuery] string module)
        {
            try
            {
                dynamic? file = null;

                if (module.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    file = await _context.pdffiles
                        .Where(f => f.Id == id)
                        .Select(f => new { f.Id, f.FilePath })
                        .FirstOrDefaultAsync();
                }
                else if (module.Equals("flash", StringComparison.OrdinalIgnoreCase))
                {
                    file = await _context.flashFiles
                        .Where(f => f.Id == id)
                        .Select(f => new { f.Id, f.FilePath })
                        .FirstOrDefaultAsync();
                }
                else if (module.Equals("video", StringComparison.OrdinalIgnoreCase))
                {
                    file = await _context.VideoFiles
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
                    _context.pdffiles.Remove(new pdfFiles { Id = file.Id });

                }
                else if (module.Equals("flash", StringComparison.OrdinalIgnoreCase))
                {
                    _context.flashFiles.Remove(new FlashFiles { Id = file.Id });
                }
                else if (module.Equals("video", StringComparison.OrdinalIgnoreCase))
                {
                    _context.VideoFiles.Remove(new videofiles { Id = file.Id });
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

        [HttpDelete("DeleteFolder")]
        public async Task<IActionResult> DeleteFolder(
        [FromQuery] int folderId,
        [FromQuery] string module)
        {
            try
            {
                // 🔍 Step 1: Find the folder with Name, Path, and ID
                var folder = await _context.Folders
                    .Where(f => f.id == folderId)
                    .Select(f => new { f.id, f.folder_name, f.folder_path })
                    .FirstOrDefaultAsync();

                if (folder == null)
                {
                    return NotFound(new { message = "Folder not found." });
                }

                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder.folder_path);
                string folderName = folder.folder_name;

                // 🔍 Step 2: Find all files in this folder (Using Folder Name Instead of ID)
                List<FileDto> files = new List<FileDto>();

                if (module.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    files = await _context.pdffiles
                        .Where(f => f.FolderName == folderName)
                        .Select(f => new FileDto { Id = f.Id, FilePath = f.FilePath }) // ✅ Explicit mapping
                        .ToListAsync();
                }
                else if (module.Equals("flash", StringComparison.OrdinalIgnoreCase))
                {
                    files = await _context.flashFiles
                        .Where(f => f.FolderName == folderName)
                        .Select(f => new FileDto { Id = f.Id, FilePath = f.FilePath }) // ✅ Explicit mapping
                        .ToListAsync();
                }
                else if (module.Equals("video", StringComparison.OrdinalIgnoreCase))
                {
                    files = await _context.VideoFiles
                        .Where(f => f.FolderName == folderName)
                        .Select(f => new FileDto { Id = f.Id, FilePath = f.FilePath }) // ✅ Explicit mapping
                        .ToListAsync();
                }


                // ✅ Step 3: Delete all files inside the folder
                foreach (var file in files)
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FilePath);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    if (module.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        var fileEntity = await _context.pdffiles.FindAsync(file.Id);
                        if (fileEntity != null) _context.pdffiles.Remove(fileEntity);
                    }
                    else if (module.Equals("flash", StringComparison.OrdinalIgnoreCase))
                    {
                        var fileEntity = await _context.flashFiles.FindAsync(file.Id);
                        if (fileEntity != null) _context.flashFiles.Remove(fileEntity);
                    }
                    else if (module.Equals("video", StringComparison.OrdinalIgnoreCase))
                    {
                        var fileEntity = await _context.VideoFiles.FindAsync(file.Id);
                        if (fileEntity != null) _context.VideoFiles.Remove(fileEntity);
                    }
                }

                // 🔥 Commit file deletions
                await _context.SaveChangesAsync();

                //  Step 4: Delete the folder from the disk
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }

                //  Step 5: Remove folder from database
                var folderEntity = await _context.Folders.FindAsync(folderId);
                if (folderEntity != null) _context.Folders.Remove(folderEntity);

                // 🔥 Commit folder deletion
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Folder '{folderName}' and all its files deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the folder.", error = ex.Message });
            }
        }
        public class FileDto
        {
            public int Id { get; set; }
            public string FilePath { get; set; }
        }

        public class UploadChunkRequest
        {
            [FromForm]
            public int classid { get; set; }
           [FromForm]
             public int subjectid { get; set; }

            [FromForm] 
            public string module { get; set; }

            [FromForm]
            public string foldername { get; set; }

            [FromForm] public int schoolid { get; set; }

            [FromForm] public IFormFile file { get; set; }

            [FromForm] public string fileName { get; set; }

            [FromForm] public int chunkIndex { get; set; }

            [FromForm] public int totalChunks { get; set; }

            [FromForm] public int adminId { get; set; }

        }
    }
}
