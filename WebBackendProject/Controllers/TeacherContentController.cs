using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using WebBackendProject.Data;
using WebBackendProject.Models;
using WebBackendProject.Models.Dto;
using WebBackendProject.Other;
using WebBackendProject.Services;

namespace WebBackendProject.Controllers
{
    [Route("api/TeacherContentController")]
    public class TeacherContentController : Controller
    {
        ApplicationDbContext _context;
        FolderAndFilesHandler _handler;
        ClassesSubjectsServices _classesSubjectsServices;
        public TeacherContentController(ApplicationDbContext context)
        {
            _context = context;
            _handler = new FolderAndFilesHandler(context);
            _classesSubjectsServices = new ClassesSubjectsServices(context);
        }
        [HttpPost("SetTeacherFolder")]
        public IActionResult SetTeacherFolder([FromBody] TeacherFolderDto folderDto)
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
            var folder = new teacherfolders
            {
                folder_name = folderDto.FolderName,
                folder_path = _handler.CreateFolder(folderDto.ClassId, folderDto.SubjectId, folderDto.Module, folderDto.FolderName,1, folderDto.schoolId),
                subject_id = folderDto.SubjectId,
                class_id = folderDto.ClassId,
                module = folderDto.Module,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
                school_id = folderDto.schoolId,
                created_by = folderDto.createdby
            };

            _context.Teacherfolders.Add(folder);
            _context.SaveChanges();

            return Ok(new { success = true, message = "Folder created successfully!" });
        }

        // Function to check if a folder already exists
        private bool IsDuplicateFolder(string folderName, int subjectId, int classId, string module)
        {
            return _context.Teacherfolders.Any(f =>
                f.folder_name == folderName &&
                f.subject_id == subjectId &&
                f.class_id == classId &&
                f.school_id == classId &&
                f.module == module
            );
        }

        public class TeacherFolderDto
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

            public int createdby { get; set; }
            
        }

        [HttpPost("GetTeacherFolders")]
        public async Task<IActionResult> GetFolderNames([FromBody] FolderRequestDto folder)
        {
            if(folder.ClassId == null)
            {
                folder.ClassId = await _classesSubjectsServices.GetStudentClassId(folder.UserId);
            }

            var folders = await _context.Teacherfolders
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

        [HttpPost("GetTeacherFiles")]
        public async Task<IActionResult> GetFiles([FromBody] FileRequestDto file)
        {
            if (file.ClassId == null)
            {
                file.ClassId = await _classesSubjectsServices.GetStudentClassId(file.UserId);
            }
            var files = new List<object>();

            if (file.Module.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                files = await _context.Teacherpdf
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
                files = await _context.Teachervideos
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

        [HttpPost("UploadteacherChunk")]
        public async Task<IActionResult> UploadChunk(
    [FromForm] TeacherUploadChunkRequest request)
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

                        if (IsDuplicateFile(request.fileName, request.foldername, request.module, request.classid, request.subjectid, request.schoolid))
                        {
                            return BadRequest(new { message = "A file with the same name already exists in this folder." });
                        }

                        if (request.module.Equals("pdf"))
                        {
                            newFile = new teacherpdf
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
                                uploaded_by_teacher = request.createdby
                            };
                            _context.Teacherpdf.Add((teacherpdf)newFile);
                        }
                        else if (request.module.Equals("video"))
                        {
                            newFile = new teachervideos
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
                               uploaded_by_teacher = request.createdby

                            };
                            _context.Teachervideos.Add((teachervideos)newFile);
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
                return _context.Teacherpdf.Any(f => f.FileName == fileName && f.FolderName == folderName && f.ClassId == classid && f.SubjectId == subjectid && f.school_id == schoolid);
            }
            else if (module.Equals("video"))
            {
                return _context.Teachervideos.Any(f => f.FileName == fileName && f.FolderName == folderName && f.ClassId == classid && f.SubjectId == subjectid && f.school_id == schoolid);
            }
            return false;
        }

        [HttpDelete("DeleteTeacherFiles")]
        public async Task<IActionResult> DeleteFiles(
    [FromQuery] int id,
    [FromQuery] string module)
        {
            try
            {
                dynamic? file = null;

                if (module.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    file = await _context.Teacherpdf
                        .Where(f => f.Id == id)
                        .Select(f => new { f.Id, f.FilePath })
                        .FirstOrDefaultAsync();
                }
                else if (module.Equals("video", StringComparison.OrdinalIgnoreCase))
                {
                    file = await _context.Teachervideos
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
                    _context.Teacherpdf.Remove(new teacherpdf { Id = file.Id });

                }
                else if (module.Equals("video", StringComparison.OrdinalIgnoreCase))
                {
                    _context.Teachervideos.Remove(new teachervideos{ Id = file.Id });
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

        [HttpDelete("DeleteTeacherFolder")]
        public async Task<IActionResult> DeleteFolder(
        [FromQuery] int folderId,
        [FromQuery] string module)
        {
            try
            {
                // 🔍 Step 1: Find the folder with Name, Path, and ID
                var folder = await _context.Teacherfolders
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
                List<TeacherFileDto> files = new List<TeacherFileDto>();

                if (module.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    files = await _context.Teacherpdf
                        .Where(f => f.FolderName == folderName)
                        .Select(f => new TeacherFileDto { Id = f.Id, FilePath = f.FilePath }) // ✅ Explicit mapping
                        .ToListAsync();
                }
                else if (module.Equals("video", StringComparison.OrdinalIgnoreCase))
                {
                    files = await _context.Teachervideos
                        .Where(f => f.FolderName == folderName)
                        .Select(f => new TeacherFileDto { Id = f.Id, FilePath = f.FilePath }) // ✅ Explicit mapping
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
                        var fileEntity = await _context.Teacherpdf.FindAsync(file.Id);
                        if (fileEntity != null) _context.Teacherpdf.Remove(fileEntity);
                    }
                    else if (module.Equals("video", StringComparison.OrdinalIgnoreCase))
                    {
                        var fileEntity = await _context.Teachervideos.FindAsync(file.Id);
                        if (fileEntity != null) _context.Teachervideos.Remove(fileEntity);
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
                var folderEntity = await _context.Teacherfolders.FindAsync(folderId);
                if (folderEntity != null) _context.Teacherfolders.Remove(folderEntity);

                // 🔥 Commit folder deletion
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Folder '{folderName}' and all its files deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the folder.", error = ex.Message });
            }
        }
        public class TeacherFileDto
        {
            public int Id { get; set; }
            public string FilePath { get; set; }
        }
        public class TeacherUploadChunkRequest
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

            [FromForm] public int createdby { get; set; }

        }
    }
}
