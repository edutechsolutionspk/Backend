using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBackendProject.Data;
using WebBackendProject.Models;
using WebBackendProject.Other;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace WebBackendProject.Controllers
{
    [Route("api/EcourseController")]
    public class EcourseController : ControllerBase
    {

        ApplicationDbContext _context;
        FolderAndFilesHandler _handler;
        public EcourseController(ApplicationDbContext context) {
            _context = context;
            _handler = new FolderAndFilesHandler(context);
        }


        [HttpPost("PostWebsiteLink")]
        public async Task<IActionResult> PostWebsiteLink([FromForm] EcourseCreateDto request)
        {

            string imagePath= "";
            if (request.link == null)
            {
                return BadRequest(new { message = "No link provided." });
            }
            if(request.bookImage == null)
            {
                return BadRequest(new { message = "No pic provided." });
            }
            try
            {
                if (request.bookImage != null && request.bookImage.Length > 0)
                {
                   
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploadedfiles/EcourseThumbnail");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);

                    }

                    string uniqueFileName = $"{Guid.NewGuid()}_{request.bookImage.FileName}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await   request.bookImage.CopyToAsync(stream);
                    }

                    imagePath = $"/uploadedfiles/EcourseThumbnails/{uniqueFileName}";
                }
                var newFile = new ecoursebook
                {
                    Name = request.fileName,
                    WebsiteLink = request.link,
                    Category = request.Category,
                    UploadDate = DateTime.UtcNow,
                    thumbnailpath = imagePath
                };

                // Ensure the category and link are valid if needed
                if (string.IsNullOrEmpty(request.fileName) || string.IsNullOrEmpty(request.link) || string.IsNullOrEmpty(request.Category))
                {
                    return BadRequest(new { message = "All fields (Category, Link, and FileName) are required." });
                }

                _context.EcourseBook.Add(newFile);
                await _context.SaveChangesAsync();

                return Ok(new { message = "File uploaded successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception details to better understand the problem
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }



        [HttpPost("GetAllFiles")]
        public async Task<IActionResult> GetAllFiles(
     [FromForm] int? userId = null,
     [FromForm] string? Category = null,
     [FromQuery] string? searchQuery = null,
     [FromQuery] int page = 1)
        {
            try
            {
                string category = Category;

                if (string.IsNullOrEmpty(category) && userId.HasValue)
                {
                    var user = await _context.Students
                        .Where(u => u.id == userId)
                        .Select(u => new { u.class_id })
                        .FirstOrDefaultAsync();

                    if (user != null)
                    {
                        category = GetCategory(user.class_id);
                    }
                }

                var query = _context.EcourseBook
                    .Where(f => string.IsNullOrEmpty(category) || f.Category == category)
                    .Select(f => new
                    {
                        Id = f.Id,
                        FileName = f.Name,
                        ThumbnailPath = f.thumbnailpath
                    });

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query = query.Where(v => v.FileName.Contains(searchQuery));
                }

                int pageSize = 15;
                int totalRecords = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                if (totalPages == 0) totalPages = 1;

                if (page < 1)
                    page = totalPages;
                else if (page > totalPages)
                    page = 1;

                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                var allfiles = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Convert relative thumbnail paths to full URLs
                var result = allfiles.Select(f => new
                {
                    f.Id,
                    f.FileName,
                    ThumbnailUrl = !string.IsNullOrEmpty(f.ThumbnailPath) ? $"{baseUrl}{f.ThumbnailPath}" : null
                });

                return Ok(new { success = true, allfiles = result, totalPages, page });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

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

        [HttpDelete("DeleteEcourseLink")]
        public async Task<IActionResult> DeleteFiles([FromQuery] int id)
        {
            try
            {
                var existingFile = await _context.EcourseBook.FindAsync(id);
                if (existingFile == null)
                {
                    return BadRequest(new { message = "The requested link was not found." });
                }

                _context.EcourseBook.Remove(existingFile);
                await _context.SaveChangesAsync();

                return Ok(new { message = "File deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the file.", error = ex.Message });
            }
        }


      public class EcourseCreateDto
        {
           
           public string Category { get; set; }
        public string link { get; set; }

        public  string fileName { get;set; }
        public IFormFile bookImage { get; set; }
        }
    }
}
