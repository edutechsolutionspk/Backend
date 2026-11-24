using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBackendProject.business.assessment.dtos;
using WebBackendProject.business.assessment.services;
using WebBackendProject.Data;
using WebBackendProject.Other;

namespace WebBackendProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssessmentController : ControllerBase
    {
        public AddAssessmentService _addAssessmentFilesService;

        private readonly FileEncryptionHelper fileEncryptionHelper = new FileEncryptionHelper();

        private readonly ApplicationDbContext _context;

        public readonly ListAssessmentService _listAssessmentFiles;

        public DeleteAssessmentService _delete;

        public AssessmentController(AddAssessmentService addAssessmentFilesService, ListAssessmentService listAssessmentFiles, DeleteAssessmentService delete, ApplicationDbContext context)
        {
            _addAssessmentFilesService = addAssessmentFilesService;
            _context = context;
            _listAssessmentFiles = listAssessmentFiles;
            _delete = delete;
        }

        [HttpPost("file")]
        public async Task<IActionResult> UploadFile([FromForm] CreateAssessmentFileDto request)
        {
            CreateAssessmentFileResponse result = await _addAssessmentFilesService.AddFile(request);
            int statusCode = result.statusCode;
            if (statusCode == 200)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("file/{fileId}")]
        public async Task<IActionResult> ViewAssessmentFile(int fileId)
        {
            var fileRecord = await (from f in _context.assessmentFiles
                                    where f.id == fileId
                                    select new { f.file_path, f.encryption_key, f.iv }).FirstOrDefaultAsync();
            if (fileRecord == null)
            {
                return NotFound("File not found.");
            }
            string encryptedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileRecord.file_path);
            if (!System.IO.File.Exists(encryptedFilePath))
            {
                return NotFound("File does not exist.");
            }
            byte[] key = Convert.FromBase64String(fileRecord.encryption_key);
            byte[] iv = Convert.FromBase64String(fileRecord.iv);
            Stream decryptedStream = fileEncryptionHelper.DecryptFileStream(encryptedFilePath, key, iv);
            Response.Headers["Content-Disposition"] = "inline";
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return new FileStreamResult(decryptedStream, "text/html");
        }

        [HttpPost("files")]
        public async Task<IActionResult> AllFiles([FromBody] AssessmentFileRequest request)
        {
            AssessmentFileResponse result = await _listAssessmentFiles.AssessmentFilesList(request);
            int statusCode = result.statusCode;
            if (statusCode == 200)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpDelete("file")]
        public async Task<IActionResult> DeleteFile([FromQuery] int id)
        {
            DeleteAssessmentFileResponse result = await _delete.DeleteFile(id);
            int statusCode = result.statusCode;
            if (statusCode == 200)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
